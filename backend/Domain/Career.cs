namespace backend.Domain;

public class Career : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ICollection<Student> Students { get; set; } = new HashSet<Student>();
}