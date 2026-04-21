using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace backend.Services.Extraction;

public static class TextNormalizer
{
    // Caracteres que el OCR confunde frecuentemente
    private static readonly (string From, string To)[] OcrFixes =
    {
        ("0", "O"), 
        ("|", "I"),
        ("l1", ""),  
        ("¡", "i"),
        ("°", "o"),
    };

    /// <summary>
    /// Normaliza el texto completo: colapsa espacios, elimina líneas vacías repetidas,
    /// quita caracteres basura típicos del OCR.
    /// </summary>
    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var lines = raw.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                       .Select(NormalizeLine)
                       .Where(l => !string.IsNullOrWhiteSpace(l));

        return string.Join("\n", lines);
    }

    public static string NormalizeLine(string line)
    {
        if (line is null) return string.Empty;

        var result = Regex.Replace(line, @"\s+", " ").Trim();

        result = Regex.Replace(result, @"[^\w\s\-\.,:;/áéíóúñüÁÉÍÓÚÑÜ@°%()]", " ");
        result = Regex.Replace(result, @"\s+", " ").Trim();

        return result;
    }

    /// <summary>
    /// Elimina tildes para comparaciones de etiquetas.
    /// </summary>
    public static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}