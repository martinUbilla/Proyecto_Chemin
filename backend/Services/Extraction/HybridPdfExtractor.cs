using SkiaSharp;
using Tesseract;
using UglyToad.PdfPig;
using PdfPage = UglyToad.PdfPig.Content.Page;

namespace backend.Services.Extraction;

public class HybridPdfExtractor : IHybridPdfExtractor, IDisposable
{
    private const string TesseractLanguages = "spa+eng+fra";

    private readonly TesseractEngine _engine;
    private readonly ILogger<HybridPdfExtractor> _logger;

    public HybridPdfExtractor(IConfiguration configuration, ILogger<HybridPdfExtractor> logger)
    {
        _logger = logger;
        var tessdataPath = configuration["Tesseract:DataPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "tessdata");

        _engine = new TesseractEngine(tessdataPath, TesseractLanguages, EngineMode.LstmOnly);
    }

    public async Task<IReadOnlyList<PageExtractionResult>> ExtractAllPagesAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        // Materializar en memoria una sola vez
        using var buffer = new MemoryStream();
        await pdfStream.CopyToAsync(buffer, cancellationToken);
        var pdfBytes = buffer.ToArray(); // Para Docnet (necesita byte[])
        buffer.Position = 0;            // Para PdfPig (necesita Stream)

        var results = new List<PageExtractionResult>();

        using var pdf = PdfDocument.Open(buffer);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PageAnalyzer.NeedsOcr(page))
            {
                var nativeText = PageAnalyzer.ExtractNativeText(page);
                _logger.LogDebug("Página {N}: texto digital ({Chars} chars)", 
                    page.Number, nativeText.Length);

                results.Add(new PageExtractionResult(
                    PageNumber:    page.Number,
                    Text:          nativeText,
                    WasOcr:        false,
                    OcrConfidence: -1f));
            }
            else
            {
                _logger.LogInformation("Página {N}: enviando a OCR", page.Number);
                var ocrResult = await RunOcrAsync(pdfBytes, page.Number);
                results.Add(ocrResult);
            }
        }

        return results;
    }

    private Task<PageExtractionResult> RunOcrAsync(byte[] pdfBytes, int pageNumber)
    {
        // Tesseract es single-threaded; Task.Run evita bloquear el hilo de ASP.NET
        return Task.Run(() =>
        {
            try
            {
                // pageIndex es 0-based en Docnet
                using var bitmap = PageRasterizer.RenderPage(pdfBytes, pageNumber - 1);
                using var pix    = BitmapToPix(bitmap);
                using var page   = _engine.Process(pix);

                return new PageExtractionResult(
                    PageNumber:    pageNumber,
                    Text:          page.GetText() ?? string.Empty,
                    WasOcr:        true,
                    OcrConfidence: page.GetMeanConfidence() * 100f);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR falló en página {N}", pageNumber);
                return new PageExtractionResult(pageNumber, string.Empty, true, 0f);
            }
        });
    }

    private static Pix BitmapToPix(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
        return Pix.LoadFromMemory(data.ToArray());
    }

    public void Dispose()
    {
        _engine.Dispose();
        GC.SuppressFinalize(this);
    }
}