using backend.Dtos;
using Microsoft.AspNetCore.Http;

namespace backend.Services;

public interface ISubmissionService
{
    Task<DocumentExtractionResultDto> ExtractDocumentDataAsync(IFormFile pdfFile, CancellationToken cancellationToken = default);
}
