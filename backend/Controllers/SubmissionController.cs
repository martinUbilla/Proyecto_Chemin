using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize(Roles = "Student,Admin")]
public class SubmissionController : ControllerBase
{
	private readonly ISubmissionService _submissionService;

	public SubmissionController(ISubmissionService submissionService)
	{
		_submissionService = submissionService;
	}

	[HttpPost]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UploadPdf([FromForm] IFormFile? pdf, CancellationToken cancellationToken)
	{
		if (pdf is null || pdf.Length == 0)
		{
			return BadRequest(new { message = "Debes seleccionar un archivo PDF." });
		}

		var isPdfByMime = pdf.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
		var isPdfByExtension = Path.GetExtension(pdf.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

		if (!isPdfByMime && !isPdfByExtension)
		{
			return BadRequest(new { message = "El archivo debe ser un PDF valido." });
		}

		var extracted = await _submissionService.ExtractDocumentDataAsync(pdf, cancellationToken);

		return Ok(new
		{
			message = "Documento procesado correctamente.",
			data = extracted
		});
	}
}
