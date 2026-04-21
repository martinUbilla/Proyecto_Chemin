using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using backend.Dtos;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;
using backend.Services.Extraction;

namespace backend.Services;

public class SubmissionService : ISubmissionService
{
	private readonly IHybridPdfExtractor _pdfExtractor;

	public SubmissionService(IHybridPdfExtractor pdfExtractor)
	{
		_pdfExtractor = pdfExtractor;
	}
	public async Task<DocumentExtractionResultDto> ExtractDocumentDataAsync(
    IFormFile pdfFile, 
    CancellationToken cancellationToken = default)
{
    
    await using var stream = pdfFile.OpenReadStream();
    using var buffer = new MemoryStream();
    await stream.CopyToAsync(buffer, cancellationToken);
    buffer.Position = 0;

    var pages = await _pdfExtractor.ExtractAllPagesAsync(buffer, cancellationToken);

    var textBuilder    = new StringBuilder();
    var ocrPageCount   = 0;
    var totalConfidence = 0f;

    foreach (var page in pages)
    {
        textBuilder.AppendLine(page.Text);
        if (page.WasOcr)
        {
            ocrPageCount++;
            totalConfidence += page.OcrConfidence;
        }
    }

   
    var rawText  = textBuilder.ToString().Trim();
    var cleaned  = TextNormalizer.Normalize(rawText);  
    var lines    = cleaned
                    .Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

   
    var studentName    = DocumentFieldExtractor.ExtractStudentName(lines);
    var rut            = ExtractRut(cleaned);
    var hostInstitution = DocumentFieldExtractor.ExtractInstitution(lines);
    var academicPeriod = DocumentFieldExtractor.ExtractAcademicPeriod(lines);

    
    var courseEntries    = DocumentFieldExtractor.ExtractCourses(lines);
    var courseCandidates = courseEntries
        .Select(c => c.Grade is not null ? $"{c.Name} — {c.Grade}" : c.Name)
        .ToList();

    
    var hasText             = !string.IsNullOrWhiteSpace(rawText);
    var averageOcrConfidence = ocrPageCount > 0 ? totalConfidence / ocrPageCount : -1f;
    var confidenceScore     = CalculateConfidenceScore(
                                hasText, studentName, rut, hostInstitution, academicPeriod);

  
    return new DocumentExtractionResultDto
    {
        FileName             = pdfFile.FileName,
        StudentName          = studentName,
        Rut                  = rut,
        HostInstitution      = hostInstitution,
        AcademicPeriod       = academicPeriod,
        CourseCandidates     = courseCandidates,
        TextPreview          = BuildTextPreview(rawText),   
        NormalizedPreview    = BuildTextPreview(cleaned),    
        HasText              = hasText,
        LikelyScanned        = ocrPageCount > 0,
        PagesTotal           = pages.Count,
        PagesOcr             = ocrPageCount,
        AverageOcrConfidence = averageOcrConfidence,
        ConfidenceScore      = confidenceScore
    };
}

	private static string BuildTextPreview(string rawText)
	{
		if (string.IsNullOrWhiteSpace(rawText))
		{
			return "No se detecto texto en el PDF.";
		}

		var collapsed = Regex.Replace(rawText, "\\s+", " ").Trim();
		return collapsed.Length <= 700 ? collapsed : collapsed[..700] + "...";
	}

	private static string? ExtractRut(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var rutRegex = new Regex("\\b(\\d{1,2}(?:\\.?\\d{3}){2}|\\d{7,8})\\s*-?\\s*([\\dkK])\\b", RegexOptions.Compiled);
		var match = rutRegex.Match(text);
		if (!match.Success)
		{
			return null;
		}

		var digits = Regex.Replace(match.Groups[1].Value, "[^0-9]", string.Empty);
		if (digits.Length < 7 || digits.Length > 8)
		{
			return match.Value;
		}

		if (digits.Length == 7)
		{
			digits = "0" + digits;
		}

		return $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}-{match.Groups[2].Value.ToUpperInvariant()}";
	}

	private static int CalculateConfidenceScore(bool hasText, string? studentName, string? rut, string? hostInstitution, string? academicPeriod)
	{
		var score = 0;

		if (hasText)
		{
			score += 30;
		}

		if (!string.IsNullOrWhiteSpace(studentName))
		{
			score += 20;
		}

		if (!string.IsNullOrWhiteSpace(rut))
		{
			score += 20;
		}

		if (!string.IsNullOrWhiteSpace(hostInstitution))
		{
			score += 15;
		}

		if (!string.IsNullOrWhiteSpace(academicPeriod))
		{
			score += 15;
		}

		return Math.Min(score, 100);
	}

	


}
