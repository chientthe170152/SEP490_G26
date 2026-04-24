using MTCA.Domain.Common;
using MTCA.Domain.Identity;
using MTCA.Domain.MasterData;

namespace MTCA.Domain.ExamBlueprints;

public class ExamBlueprint : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int SubjectId { get; set; }
    public string? OwnerId { get; set; }
    public int? CurrentVersionId { get; set; }

    public Subject Subject { get; set; } = default!;
    public ApplicationUser? Owner { get; set; }
    public ExamBlueprintVersion? CurrentVersion { get; set; }
    public ICollection<ExamBlueprintVersion> Versions { get; set; } = new List<ExamBlueprintVersion>();
}
