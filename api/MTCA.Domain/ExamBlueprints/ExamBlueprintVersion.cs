using MTCA.Domain.Common;
using MTCA.Domain.ExamBlueprints.Enums;

namespace MTCA.Domain.ExamBlueprints;

public class ExamBlueprintVersion : AuditableEntity
{
    public int Id { get; set; }
    public int BlueprintId { get; set; }
    public int VersionNumber { get; set; }
    public ExamBlueprintStatus Status { get; set; } = ExamBlueprintStatus.DRAFT;
    public decimal TotalScore { get; set; } = 10.00m;
    public int TotalQuestions { get; set; }
    public int? DurationMin { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public ExamBlueprint Blueprint { get; set; } = default!;
    public ICollection<BlueprintCell> Cells { get; set; } = new List<BlueprintCell>();
}
