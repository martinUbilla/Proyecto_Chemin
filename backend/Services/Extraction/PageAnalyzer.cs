using UglyToad.PdfPig.Content;

namespace backend.Services.Extraction;

public static class PageAnalyzer
{
    private const int MinAlphanumericForDigital = 50;

    /// <summary>
    /// Una página necesita OCR si tiene pocas letras nativas Y tiene imágenes embebidas,
    /// o si directamente no tiene texto en absoluto (página completamente escaneada).
    /// </summary>
    public static bool NeedsOcr(Page page)
    {
        var nativeText  = page.Text ?? string.Empty;
        var alphaCount  = nativeText.Count(char.IsLetterOrDigit);
        var hasImages   = page.GetImages().Any();

        // Caso 1: Página puramente escaneada — sin texto digital y con imagen
        if (alphaCount < MinAlphanumericForDigital && hasImages)
            return true;

        // Caso 2: Sin texto y sin imágenes detectables (PDF vacío o corrupto)
        if (alphaCount == 0)
            return true;

        return false;
    }

    /// <summary>
    /// Extrae texto nativo de forma segura.
    /// </summary>
    public static string ExtractNativeText(Page page)
    {
        try { return page.Text ?? string.Empty; }
        catch { return string.Empty; }
    }
}