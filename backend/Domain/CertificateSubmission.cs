namespace backend.Domain;

public class CertificateSubmission : BaseEntity
{
    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int ExchangePeriodId { get; set; }

    public ExchangePeriod ExchangePeriod { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Status { get; set; } = "PendingReview";

    public string? ReviewNotes { get; set; }

    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAtUtc { get; set; }

    public ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
}