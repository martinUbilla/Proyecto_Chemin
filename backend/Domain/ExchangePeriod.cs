namespace backend.Domain;

public class ExchangePeriod : BaseEntity
{
    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int CampusId { get; set; }

    public Campus Campus { get; set; } = null!;

    public string AcademicYear { get; set; } = string.Empty;

    public string Semester { get; set; } = string.Empty;

    public string HostCountry { get; set; } = string.Empty;

    public string HostInstitution { get; set; } = string.Empty;

    public DateTimeOffset? DepartureDateUtc { get; set; }

    public DateTimeOffset? ReturnDateUtc { get; set; }

    public bool IsCurrent { get; set; } = true;

    public ICollection<CertificateSubmission> CertificateSubmissions { get; set; } = new HashSet<CertificateSubmission>();
}