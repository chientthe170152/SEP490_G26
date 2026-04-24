using MTCA.Domain.Common;
using MTCA.Domain.ExamBlueprints;
using MTCA.Domain.Exams.Enums;
using MTCA.Domain.MasterData;

namespace MTCA.Domain.Exams;

public class Exam : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SubjectId { get; set; }
    public int BlueprintVersionId { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.DRAFT;
    public string? Password { get; set; }
    public int DurationMin { get; set; }
    public ResultVisibilityTiming ResultVisibilityTiming { get; set; } = ResultVisibilityTiming.AFTER_SESSION_CLOSE;
    public bool ShowTotalScore { get; set; } = true;
    public bool ShowCorrectAnswers { get; set; }
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;
    public int VariantCount { get; set; } = 1;

    public Subject Subject { get; set; } = default!;
    public ExamBlueprintVersion BlueprintVersion { get; set; } = default!;
    public ICollection<ExamVariant> Variants { get; set; } = new List<ExamVariant>();
}
