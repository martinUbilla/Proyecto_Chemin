// Services/Extraction/DocumentFieldExtractor.cs
using System.Text.RegularExpressions;

namespace backend.Services.Extraction;

/// <summary>
/// Extrae campos semánticos usando estrategias en cascada:
/// 1. Búsqueda por etiqueta conocida (inline y siguiente línea)
/// 2. Patrones estructurales del documento
/// 3. Heurísticas de último recurso
/// </summary>
public static class DocumentFieldExtractor
{
    // ── Diccionarios de etiquetas ──────────────────────────────────────────
    public record CourseEntry(
    string Name,
    float? Grade
);

    private static readonly string[] NameLabels =
        { "alumno", "nombre alumno", "nombre estudiante", "estudiante",
          "student", "student name", "nombre", "nome" };

    private static readonly string[] InstitutionLabels =
        { "institucion de destino", "institucion acogida", "universidad de destino",
          "host institution", "destination university", "university", "universidad",
          "etablissement", "institution" };

    private static readonly string[] PeriodLabels =
        { "periodo academico", "periodo de intercambio", "semestre", "term",
          "academic period", "periode", "año academico" };

    private static readonly string[] CourseLabels =
        { "asignatura", "asignaturas", "curso", "cursos", "ramo", "ramos",
          "course", "courses", "subject", "subjects", "materia", "materias",
          "module", "modules", "uc", "unidad curricular" };

    private static readonly string[] GradeLabels =
        { "nota", "notas", "calificacion", "calificaciones", "grade", "grades",
          "mark", "marks", "note", "notes", "resultado" };

    // ── API pública ────────────────────────────────────────────────────────

    public static string? ExtractStudentName(IReadOnlyList<string> lines)
        => ExtractByLabel(lines, NameLabels, minLen: 5, maxLen: 90)
        ?? ExtractFromCertificatePhrase(lines)
        ?? ExtractNameHeuristic(lines);

    public static string? ExtractInstitution(IReadOnlyList<string> lines)
        => ExtractByLabel(lines, InstitutionLabels, minLen: 4, maxLen: 140)
        ?? ExtractInstitutionPattern(lines);

    public static string? ExtractAcademicPeriod(IReadOnlyList<string> lines)
        => ExtractByLabel(lines, PeriodLabels, minLen: 2, maxLen: 80)
        ?? ExtractPeriodPattern(lines);

    public static List<CourseEntry> ExtractCourses(IReadOnlyList<string> lines)
    {
        // Intentar primero extracción tabular (más precisa)
        var tabular = ExtractTabularCourses(lines);
        if (tabular.Count > 0) return tabular;

        // Fallback: extracción por sección
        return ExtractSectionCourses(lines);
    }

    // ── Extracción por etiqueta ────────────────────────────────────────────

    private static string? ExtractByLabel(
        IReadOnlyList<string> lines,
        string[] labels,
        int minLen,
        int maxLen)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var normalized = TextNormalizer.RemoveDiacritics(lines[i]);

            foreach (var label in labels)
            {
                var normalizedLabel = TextNormalizer.RemoveDiacritics(label);
                if (!normalized.Contains(normalizedLabel)) continue;

                // Estrategia A: valor en la misma línea tras separador
                var inline = ExtractInlineValue(lines[i], minLen, maxLen);
                if (inline is not null) return inline;

                // Estrategia B: valor en la línea siguiente
                if (i + 1 < lines.Count)
                {
                    var next = lines[i + 1].Trim();
                    if (IsPlausibleValue(next, minLen, maxLen) 
                        && !ContainsAnyLabel(next, labels))
                        return next;
                }

                // Estrategia C: valor dos líneas más abajo (tablas con celda vacía intermedia)
                if (i + 2 < lines.Count)
                {
                    var next2 = lines[i + 2].Trim();
                    if (IsPlausibleValue(next2, minLen, maxLen)
                        && !ContainsAnyLabel(next2, labels))
                        return next2;
                }
            }
        }
        return null;
    }

    private static string? ExtractInlineValue(string line, int minLen, int maxLen)
    {
        // Soporta separadores: "Alumno: Juan" | "Alumno - Juan" | "Alumno | Juan"
        var match = Regex.Match(line, @"[:|\-–]\s*(.+)$");
        if (!match.Success) return null;

        var value = match.Groups[1].Value.Trim();
        return IsPlausibleValue(value, minLen, maxLen) ? value : null;
    }

    // ── Extracción de nombre por patrones semánticos ───────────────────────

    private static string? ExtractFromCertificatePhrase(IReadOnlyList<string> lines)
    {
        // "certifica que NOMBRE RUT..." / "certify that NAME has completed..."
        var pattern = new Regex(
            @"certifica(?:mos)?(?:\s+que)?|certif(?:y|ies)\s+that",
            RegexOptions.IgnoreCase);

        foreach (var line in lines)
        {
            if (!pattern.IsMatch(line)) continue;

            // El nombre viene inmediatamente después de la frase
            var afterPhrase = pattern.Replace(line, "").Trim();
            // Cortar antes del RUT o de otra cláusula
            var name = Regex.Match(afterPhrase,
                @"^([A-ZÁÉÍÓÚÑ][a-záéíóúñ]+(?:\s+[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+){1,4})");
            if (name.Success) return name.Groups[1].Value;
        }
        return null;
    }

    private static string? ExtractNameHeuristic(IReadOnlyList<string> lines)
    {
        // Busca líneas con 2-5 palabras capitalizadas, sin dígitos, longitud razonable
        var namePattern = new Regex(
            @"^([A-ZÁÉÍÓÚÑ][a-záéíóúñ']+(?:\s+[A-ZÁÉÍÓÚÑ][a-záéíóúñ']+){1,4})$");

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Any(char.IsDigit)) continue;
            if (ContainsAnyLabel(trimmed, CourseLabels)) continue;
            if (ContainsAnyLabel(trimmed, InstitutionLabels)) continue;

            var match = namePattern.Match(trimmed);
            if (match.Success && match.Value.Length is >= 8 and <= 80)
                return match.Value;
        }
        return null;
    }

    // ── Extracción de institución ──────────────────────────────────────────

    private static string? ExtractInstitutionPattern(IReadOnlyList<string> lines)
    {
        var pattern = new Regex(
            @"\b(Universidad|University|Instituto|Institute|Université|Hochschule|Universität)\b.{0,100}",
            RegexOptions.IgnoreCase);

        foreach (var line in lines)
        {
            var match = pattern.Match(line);
            if (match.Success)
                return match.Value.Trim().TrimEnd('.', ',');
        }
        return null;
    }

    // ── Extracción de período ──────────────────────────────────────────────

    private static string? ExtractPeriodPattern(IReadOnlyList<string> lines)
    {
        // "Primer semestre 2023" / "2023-1" / "Fall 2023" / "Semester 1, 2023"
        var patterns = new[]
        {
            new Regex(@"(?:primer|segundo|1er|2do|first|second|fall|spring|winter|summer|autumn)\s+(?:semestre|semester|term)\s+(?:de\s+)?(20\d{2})", RegexOptions.IgnoreCase),
            new Regex(@"\b(20\d{2})\s*[-/]\s*(20\d{2}|[12])\b"),
            new Regex(@"\b(20\d{2})\s*[-]\s*(1|2)\b"),
        };

        foreach (var line in lines)
        foreach (var pattern in patterns)
        {
            var match = pattern.Match(line);
            if (match.Success) return match.Value;
        }
        return null;
    }

    // ── Extracción de cursos: modo tabular ─────────────────────────────────
    // Detecta cuando el texto tiene estructura "Curso | Nota" o columnas separadas por espacios

    private static List<CourseEntry> ExtractTabularCourses(IReadOnlyList<string> lines)
    {
        var results = new List<CourseEntry>();

        // Patrón: "Nombre del curso    5.5" o "Nombre del curso | 5,5 | Aprobado"
        var linePattern = new Regex(
            @"^(.{5,80}?)\s{2,}([\d][.,]\d{1,2}|\d{1,3}(?:[.,]\d{1,2})?)\s*$");
        var pipePattern = new Regex(
            @"^([^|]{5,80})\|\s*([\d][.,]\d{1,2}|\d{1,3}(?:[.,]\d{1,2})?)\s*(?:\|.*)?$");

        var inSection = false;

        foreach (var line in lines)
        {
            if (ContainsAnyLabel(line, CourseLabels))
            {
                inSection = true;
                continue;
            }
            if (!inSection) continue;
            if (IsSectionEnd(line)) { inSection = false; continue; }

            var match = linePattern.Match(line) is { Success: true } m1 ? m1
                      : pipePattern.Match(line) is { Success: true } m2 ? m2
                      : null;

            if (match is null) continue;

            var name  = CleanCourseName(match.Groups[1].Value);
            var grade = ParseGrade(match.Groups[2].Value);

            if (name.Length >= 4)
                results.Add(new CourseEntry(name, grade));
        }

        return results;
    }

    // ── Extracción de cursos: modo sección (sin columna de nota) ──────────

    private static List<CourseEntry> ExtractSectionCourses(IReadOnlyList<string> lines)
    {
        var results  = new List<CourseEntry>();
        var inSection = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (ContainsAnyLabel(line, CourseLabels))
            {
                inSection = true;
                continue;
            }
            if (!inSection) continue;
            if (IsSectionEnd(line)) { inSection = false; continue; }

            var name = CleanCourseName(StripLeadingBulletOrCode(line));
            if (name.Length < 4 || name.Split(' ').Length < 2) continue;

            // Buscar nota en la misma línea o en la siguiente
            var grade = TryExtractGradeFromLine(line)
                     ?? (i + 1 < lines.Count ? TryExtractGradeFromLine(lines[i + 1]) : null);

            results.Add(new CourseEntry(name, grade));
        }

        return results
            .DistinctBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static bool IsSectionEnd(string line)
    {
        var n = TextNormalizer.RemoveDiacritics(line);
        return n.Contains("firma") || n.Contains("signature")
            || n.Contains("observacion") || n.Contains("total")
            || n.Contains("promedio") || n.Contains("average")
            || ContainsAnyLabel(line, NameLabels)
            || ContainsAnyLabel(line, InstitutionLabels);
    }

    private static float? TryExtractGradeFromLine(string line)
    {
        var match = Regex.Match(line, @"\b(\d{1,2}[.,]\d{1,2}|\d{1,3})\b");
        return match.Success ? ParseGrade(match.Value) : null;
    }

    private static float? ParseGrade(string raw)
    {
        var normalized = raw.Replace(',', '.');
        return float.TryParse(normalized,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result) ? result : null;
    }

    private static string CleanCourseName(string value)
        => Regex.Replace(value, @"\s+", " ").Trim().TrimEnd('.', ',', ':');

    private static string StripLeadingBulletOrCode(string line)
        => Regex.Replace(line, @"^\s*(?:[-•*◦▪]|\d+[.):]|[A-Z]{2,6}[-_]?\d{2,6})\s*", "").Trim();

    private static bool IsPlausibleValue(string value, int minLen, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var cleaned = value.Trim();
        return cleaned.Length >= minLen && cleaned.Length <= maxLen
            && !Regex.IsMatch(cleaned, @"^(?:si|no|yes|ok|n/a|x)$", RegexOptions.IgnoreCase);
    }

    private static bool ContainsAnyLabel(string line, string[] labels)
    {
        var n = TextNormalizer.RemoveDiacritics(line);
        return labels.Any(l => n.Contains(TextNormalizer.RemoveDiacritics(l)));
    }
}