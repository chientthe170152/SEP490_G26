namespace MTCA.Domain.MasterData;

public class Chapter
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int OrderIndex { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Subject Subject { get; set; } = default!;
}
