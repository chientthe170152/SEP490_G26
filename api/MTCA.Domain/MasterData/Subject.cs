using MTCA.Domain.Common;

namespace MTCA.Domain.MasterData;

public class Subject : BaseEntity<int>
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
