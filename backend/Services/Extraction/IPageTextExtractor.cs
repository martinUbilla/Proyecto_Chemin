namespace backend.Services.Extraction;

public record PageExtractionResult(
    int PageNumber,
    string Text,
    bool WasOcr,
    float OcrConfidence  // 0-100, -1 si no aplica
);

public interface IHybridPdfExtractor
{
    Task<IReadOnlyList<PageExtractionResult>> ExtractAllPagesAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default);
}