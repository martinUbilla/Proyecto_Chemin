namespace backend.Domain;

public class Notification : BaseEntity
{
    public int? StudentId { get; set; }

    public Student? Student { get; set; }

    public int? CertificateSubmissionId { get; set; }

    public CertificateSubmission? CertificateSubmission { get; set; }

    public string RecipientRole { get; set; } = "Student";

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }
}