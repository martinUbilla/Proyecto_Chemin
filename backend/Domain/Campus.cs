namespace backend.Domain;

public class Campus : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ICollection<Student> Students { get; set; } = new HashSet<Student>();

    public ICollection<ExchangePeriod> ExchangePeriods { get; set; } = new HashSet<ExchangePeriod>();
}