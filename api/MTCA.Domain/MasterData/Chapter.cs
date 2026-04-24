using MTCA.Domain.Common;

namespace MTCA.Domain.MasterData;

public class Chapter : BaseEntity<int>
{
    public int SubjectId { get; set; }
    public int OrderIndex { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Subject Subject { get; set; } = default!;
}
