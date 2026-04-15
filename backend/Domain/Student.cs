namespace backend.Domain;

public class Student : BaseEntity
{
    public string StudentCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public int CampusId { get; set; }

    public Campus Campus { get; set; } = null!;

    public int CareerId { get; set; }

    public Career Career { get; set; } = null!;

    public int CurrentYear { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ExchangePeriod> ExchangePeriods { get; set; } = new HashSet<ExchangePeriod>();

    public ICollection<CertificateSubmission> CertificateSubmissions { get; set; } = new HashSet<CertificateSubmission>();

    public ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
}