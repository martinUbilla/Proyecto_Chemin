namespace backend.Dtos;

public class DocumentExtractionResultDto
{
    public string FileName { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public string? Rut { get; set; }
    public string? HostInstitution { get; set; }
    public string? AcademicPeriod { get; set; }
    public List<string> CourseCandidates { get; set; } = new();
    public string TextPreview { get; set; } = string.Empty;
    public string NormalizedPreview { get; set; } = string.Empty;
    public bool HasText { get; set; }
    public bool LikelyScanned { get; set; }
    public int ConfidenceScore { get; set; }
}