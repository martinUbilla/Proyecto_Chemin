using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using backend.Dtos;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;

namespace backend.Services;

public class SubmissionService : ISubmissionService
{
	private static readonly string[] StudentNameLabels =
	new[]
	{
		"alumno",
		"nombre alumno",
		"nombre estudiante",
		"nombre y estudiante",
		"estudiante",
		"student",
		"student name",
		"nombre"
	};

	private static readonly string[] HostInstitutionLabels =
	new[]
	{
		"institucion de destino",
		"institucion destino",
		"institucion de acogida",
		"universidad de destino",
		"host institution",
		"destination university",
		"universidad",
		"university"
	};

	private static readonly string[] AcademicPeriodLabels =
	new[]
	{
		"periodo academico",
		"periodo",
		"periodo de intercambio",
		"semestre",
		"term",
		"academic period"
	};

	private static readonly string[] CourseSectionLabels =
	new[]
	{
		"asignaturas",
		"asignatura",
		"cursos",
		"curso",
		"courses",
		"course",
		"subjects",
		"ramo",
		"ramos"
	};

	public async Task<DocumentExtractionResultDto> ExtractDocumentDataAsync(IFormFile pdfFile, CancellationToken cancellationToken = default)
	{
		await using var stream = pdfFile.OpenReadStream();

		using var buffer = new MemoryStream();
		await stream.CopyToAsync(buffer, cancellationToken);
		buffer.Position = 0;

		var textBuilder = new StringBuilder();
		using (var pdf = PdfDocument.Open(buffer))
		{
			foreach (var page in pdf.GetPages())
			{
				textBuilder.AppendLine(page.Text);
			}
		}

		var rawText = textBuilder.ToString().Trim();
		var lines = BuildLines(rawText);
		var normalizedText = NormalizeForSearch(rawText);
		var studentName = ExtractStudentName(rawText, lines);
		var rut = ExtractRut(rawText);
		var hostInstitution = ExtractHostInstitution(rawText, lines);
		var academicPeriod = ExtractAcademicPeriod(rawText, lines);
		var courseCandidates = ExtractCourseCandidates(lines);
		var hasText = !string.IsNullOrWhiteSpace(rawText);
		var confidenceScore = CalculateConfidenceScore(hasText, studentName, rut, hostInstitution, academicPeriod);

		return new DocumentExtractionResultDto
		{
			FileName = pdfFile.FileName,
			StudentName = studentName,
			Rut = rut,
			HostInstitution = hostInstitution,
			AcademicPeriod = academicPeriod,
			CourseCandidates = courseCandidates,
			TextPreview = BuildTextPreview(rawText),
			NormalizedPreview = BuildTextPreview(normalizedText),
			HasText = hasText,
			LikelyScanned = !hasText || rawText.Length < 180,
			ConfidenceScore = confidenceScore
		};
	}

	private static string BuildTextPreview(string rawText)
	{
		if (string.IsNullOrWhiteSpace(rawText))
		{
			return "No se detecto texto en el PDF. Si es un escaneo, el siguiente paso es integrar OCR de imagen.";
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

	private static string? ExtractStudentName(string text, IReadOnlyList<string> lines)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var byLabel = ExtractByLabels(lines, StudentNameLabels, 4, 90);
		if (!string.IsNullOrWhiteSpace(byLabel))
		{
			return CleanupValue(byLabel);
		}

		var certificateRegex = new Regex(
			"(?:certifica(?:mos)?(?:\\s+que)?|se\\s+certifica\\s+que|certify\\s+that|certifies\\s+that)\\s+(?<value>[a-záéíóúñ'\\-\\s]{6,100}?)(?:,|\\s+rut|\\s+run|\\s+identificad|\\s+ha\\s+cursado|\\s+has\\s+completed|\\.)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		var certificateMatch = certificateRegex.Match(text);
		if (certificateMatch.Success)
		{
			return CleanupValue(certificateMatch.Groups["value"].Value);
		}

		var genericNameRegex = new Regex("\\b(?<value>[a-záéíóúñ][a-záéíóúñ'\\-]+(?:\\s+[a-záéíóúñ][a-záéíóúñ'\\-]+){1,4})\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		var genericMatches = genericNameRegex.Matches(text);
		foreach (Match genericMatch in genericMatches)
		{
			var candidate = CleanupValue(genericMatch.Groups["value"].Value);
			if (candidate.Length >= 8 && candidate.Length <= 90 && !ContainsAnyLabel(candidate, HostInstitutionLabels))
			{
				return candidate;
			}
		}

		return null;
	}

	private static string? ExtractHostInstitution(string text, IReadOnlyList<string> lines)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var byLabel = ExtractByLabels(lines, HostInstitutionLabels, 4, 120);
		if (!string.IsNullOrWhiteSpace(byLabel))
		{
			return CleanupValue(byLabel);
		}

		var fallbackRegex = new Regex(
			"(?:en\\s+la\\s+universidad(?:\\s+de)?|at\\s+the\\s+university(?:\\s+of)?|host\\s+institution\\s*[:\\-]?)\\s+(?<value>[^,;\\.\\r\\n]{4,140})",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		var match = fallbackRegex.Match(text);
		if (match.Success)
		{
			return CleanupValue(match.Groups["value"].Value);
		}

		var institutionRegex = new Regex("\\b(?:Universidad|University|Instituto|Institute)\\s+(?:de\\s+|of\\s+)?(?<value>[A-Za-zÀ-ÿ'\\-\\s]{3,110})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		var institutionMatch = institutionRegex.Match(text);
		if (institutionMatch.Success)
		{
			return CleanupValue($"{institutionMatch.Value}");
		}

		return null;
	}

	private static string? ExtractAcademicPeriod(string text, IReadOnlyList<string> lines)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		var byLabel = ExtractByLabels(lines, AcademicPeriodLabels, 2, 80);
		if (!string.IsNullOrWhiteSpace(byLabel))
		{
			return CleanupValue(byLabel);
		}

		var rangeRegex = new Regex("\\b(20\\d{2})\\s*[-/]\\s*(20\\d{2}|[12])\\b", RegexOptions.Compiled);
		var rangeMatch = rangeRegex.Match(text);
		if (rangeMatch.Success)
		{
			return rangeMatch.Value;
		}

		var semesterRegex = new Regex("(?:primer|segundo|1er|2do|first|second)\\s+(?:semestre|semester)\\s+(?:de\\s+)?(20\\d{2})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		var semesterMatch = semesterRegex.Match(text);
		if (semesterMatch.Success)
		{
			return semesterMatch.Value;
		}

		var monthYearRegex = new Regex("(?:enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|octubre|noviembre|diciembre|january|february|march|april|may|june|july|august|september|october|november|december)\\s+\\d{4}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		var monthYearMatches = monthYearRegex.Matches(text);
		if (monthYearMatches.Count >= 2)
		{
			return $"{monthYearMatches[0].Value} - {monthYearMatches[1].Value}";
		}

		return null;
	}

	private static IReadOnlyList<string> BuildLines(string text)
	{
		return text
			.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
			.Select(line => Regex.Replace(line ?? string.Empty, "\\s+", " ").Trim())
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.ToList();
	}

	private static string? ExtractByLabels(IReadOnlyList<string> lines, IReadOnlyList<string> labels, int minLength, int maxLength)
	{
		for (var i = 0; i < lines.Count; i++)
		{
			var line = lines[i];
			var normalizedLine = NormalizeForSearch(line);

			foreach (var label in labels)
			{
				var normalizedLabel = NormalizeForSearch(label);
				if (!normalizedLine.Contains(normalizedLabel))
				{
					continue;
				}

				var inlineValue = TryExtractInlineValue(line, minLength, maxLength);
				if (!string.IsNullOrWhiteSpace(inlineValue))
				{
					return inlineValue;
				}

				if (i + 1 < lines.Count)
				{
					var nextLine = lines[i + 1];
					if (LooksLikeValue(nextLine, minLength, maxLength) && !ContainsAnyLabel(nextLine, labels))
					{
						return nextLine;
					}
				}
			}
		}

		return null;
	}

	private static List<string> ExtractCourseCandidates(IReadOnlyList<string> lines)
	{
		var candidates = new List<string>();
		var inCourseSection = false;

		for (var i = 0; i < lines.Count; i++)
		{
			var line = lines[i];
			var normalizedLine = NormalizeForSearch(line);

			if (ContainsAnyLabel(line, CourseSectionLabels))
			{
				inCourseSection = true;

				var inlineCourse = TryExtractInlineValue(line, 4, 120);
				if (!string.IsNullOrWhiteSpace(inlineCourse) && IsLikelyCourseLine(inlineCourse))
				{
					candidates.Add(CleanupValue(inlineCourse));
				}

				continue;
			}

			if (inCourseSection)
			{
				if (IsSectionBreak(normalizedLine))
				{
					inCourseSection = false;
					continue;
				}

				if (IsLikelyCourseLine(line))
				{
					candidates.Add(CleanupValue(RemoveLeadingBulletOrCode(line)));
				}
			}

			if (TryExtractStandaloneCourse(line, out var standaloneCourse))
			{
				candidates.Add(CleanupValue(standaloneCourse));
			}
		}

		return candidates
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(12)
			.ToList();
	}

	private static bool IsSectionBreak(string normalizedLine)
	{
		return normalizedLine.Contains("firma")
			|| normalizedLine.Contains("signature")
			|| normalizedLine.Contains("observacion")
			|| normalizedLine.Contains("observacion")
			|| normalizedLine.Contains("periodo academico")
			|| normalizedLine.Contains("institucion")
			|| normalizedLine.Contains("nombre estudiante")
			|| normalizedLine.Contains("rut");
	}

	private static bool IsLikelyCourseLine(string line)
	{
		var cleaned = CleanupValue(RemoveLeadingBulletOrCode(line));
		if (cleaned.Length < 5 || cleaned.Length > 140)
		{
			return false;
		}

		if (ContainsAnyLabel(cleaned, StudentNameLabels)
			|| ContainsAnyLabel(cleaned, HostInstitutionLabels)
			|| ContainsAnyLabel(cleaned, AcademicPeriodLabels)
			|| ContainsAnyLabel(cleaned, CourseSectionLabels))
		{
			return false;
		}

		if (Regex.IsMatch(cleaned, "^(?:si|no|yes|ok|n/a)$", RegexOptions.IgnoreCase))
		{
			return false;
		}

		var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return words.Length >= 2;
	}

	private static bool TryExtractStandaloneCourse(string line, out string course)
	{
		course = string.Empty;
		var match = Regex.Match(line, "(?:asignatura|curso|subject|course)\\s*[:\\-]\\s*(?<value>[^;\\r\\n]{4,140})", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return false;
		}

		var value = CleanupValue(match.Groups["value"].Value);
		if (!IsLikelyCourseLine(value))
		{
			return false;
		}

		course = value;
		return true;
	}

	private static string RemoveLeadingBulletOrCode(string line)
	{
		return Regex.Replace(line, "^\\s*(?:[-•*]|\\d+[\\.)]|[A-Z]{2,5}[-_]?\\d{2,5})\\s*", string.Empty).Trim();
	}

	private static string? TryExtractInlineValue(string line, int minLength, int maxLength)
	{
		var separators = new[] { ':', '-', '|' };
		var separatorIndex = line.IndexOfAny(separators);
		if (separatorIndex < 0 || separatorIndex == line.Length - 1)
		{
			return null;
		}

		var value = CleanupValue(line[(separatorIndex + 1)..]);
		return LooksLikeValue(value, minLength, maxLength) ? value : null;
	}

	private static bool LooksLikeValue(string value, int minLength, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		var cleaned = CleanupValue(value);
		if (cleaned.Length < minLength || cleaned.Length > maxLength)
		{
			return false;
		}

		if (Regex.IsMatch(cleaned, "^(?:si|no|yes|ok)$", RegexOptions.IgnoreCase))
		{
			return false;
		}

		return true;
	}

	private static bool ContainsAnyLabel(string line, IReadOnlyList<string> labels)
	{
		var normalizedLine = NormalizeForSearch(line);
		return labels.Any(label => normalizedLine.Contains(NormalizeForSearch(label)));
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

	private static string CleanupValue(string value)
	{
		return Regex.Replace(value, "\\s+", " ").Trim().Trim(':', '-', ',', '.');
	}

	private static string NormalizeForSearch(string value)
	{
		var normalized = value.Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder();
		foreach (var c in normalized)
		{
			var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
			if (unicodeCategory != UnicodeCategory.NonSpacingMark)
			{
				sb.Append(char.ToLowerInvariant(c));
			}
		}

		return sb.ToString().Normalize(NormalizationForm.FormC);
	}
}
