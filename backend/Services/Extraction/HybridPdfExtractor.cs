using SkiaSharp;
using Tesseract;
using UglyToad.PdfPig;
// Alias explícito para evitar ambigüedad con otros namespaces que tengan "Page"
using PdfPage = UglyToad.PdfPig.Content.Page;

namespace backend.Services.Extraction;

public class HybridPdfExtractor : IHybridPdfExtractor, IDisposable
{
    private const int MinCharsToSkipOcr = 50;
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
        using var buffer = new MemoryStream();
        await pdfStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var results = new List<PageExtractionResult>();

        using var pdf = PdfDocument.Open(buffer);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // "page" ahora es inequívocamente UglyToad.PdfPig.Content.Page
            var nativeText = ExtractNativeText(page);
            var alphanumericCount = nativeText.Count(char.IsLetterOrDigit);

            if (alphanumericCount >= MinCharsToSkipOcr)
            {
                results.Add(new PageExtractionResult(
                    PageNumber: page.Number,
                    Text: nativeText,
                    WasOcr: false,
                    OcrConfidence: -1f));
            }
            else
            {
                _logger.LogInformation(
                    "Página {Page} tiene solo {Chars} caracteres nativos. Corriendo OCR.",
                    page.Number, alphanumericCount);

                var ocrResult = await RunOcrOnPageAsync(buffer, page.Number);
                results.Add(ocrResult);
            }
        }

        return results;
    }

    // Alias resuelve la ambigüedad aquí también
    private static string ExtractNativeText(PdfPage page)
    {
        try
        {
            return page.Text ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private Task<PageExtractionResult> RunOcrOnPageAsync(MemoryStream pdfBuffer, int pageNumber)
    {
        return Task.Run(() =>
        {
            try
            {
                using var skBitmap = RasterizePage(pdfBuffer, pageNumber);
                using var pix = ConvertSkBitmapToPix(skBitmap);
                using var page = _engine.Process(pix);

                var text = page.GetText();
                var confidence = page.GetMeanConfidence() * 100f;

                return new PageExtractionResult(
                    PageNumber: pageNumber,
                    Text: text ?? string.Empty,
                    WasOcr: true,
                    OcrConfidence: confidence);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "OCR falló en página {Page}. Se continuará sin texto.",
                    pageNumber);

                return new PageExtractionResult(
                    PageNumber: pageNumber,
                    Text: string.Empty,
                    WasOcr: true,
                    OcrConfidence: 0f);
            }
        });
    }

    // SkiaSharp reemplaza System.Drawing — funciona en Windows, Linux y macOS
    private static SKBitmap RasterizePage(MemoryStream pdfBuffer, int pageNumber)
    {
        pdfBuffer.Position = 0;
        using var pdf = PdfDocument.Open(pdfBuffer);

        var page = pdf.GetPages().ElementAt(pageNumber - 1);

        const float dpi = 300f;
        const float scale = dpi / 72f;

        var width = (int)(page.Width * scale);
        var height = (int)(page.Height * scale);

        var bitmap = new SKBitmap(width, height, SKColorType.Rgb888x, SKAlphaType.Opaque);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        // Extraemos imágenes embebidas de la página (para PDFs escaneados)
        foreach (var pdfImage in page.GetImages())
        {
            try
            {
                if (pdfImage.TryGetPng(out var pngBytes))
                {
                    using var skImage = SKImage.FromEncodedData(pngBytes);
                    if (skImage is not null)
                    {
                        var destRect = new SKRect(0, 0, width, height);
                        canvas.DrawImage(skImage, destRect);
                    }
                }
            }
            catch
            {
                // Imagen individual corrupta, continuamos con las demás
            }
        }

        return bitmap;
    }

    private static Pix ConvertSkBitmapToPix(SKBitmap bitmap)
    {
        // Codificamos a PNG en memoria y Tesseract lo carga desde bytes
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return Pix.LoadFromMemory(data.ToArray());
    }

    public void Dispose()
    {
        _engine.Dispose();
        GC.SuppressFinalize(this);
    }
}