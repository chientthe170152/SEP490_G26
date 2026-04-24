using MTCA.Domain.Common;
using MTCA.Domain.MasterData;
using MTCA.Domain.QuestionBank.Enums;

namespace MTCA.Domain.ExamBlueprints;

public class BlueprintCell : BaseEntity<int>
{
    public int BlueprintVersionId { get; set; }
    public int ChapterId { get; set; }
    public BloomLevel BloomLevel { get; set; }
    public int QuestionCount { get; set; }
    public decimal ScorePerQuestion { get; set; }
    public double? DifficultyHint { get; set; }

    public ExamBlueprintVersion BlueprintVersion { get; set; } = default!;
    public Chapter Chapter { get; set; } = default!;
}
