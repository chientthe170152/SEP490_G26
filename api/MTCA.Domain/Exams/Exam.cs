using MTCA.Domain.Common;
using MTCA.Domain.ExamBlueprints;
using MTCA.Domain.Exams.Enums;
using MTCA.Domain.MasterData;

namespace MTCA.Domain.Exams;

public class Exam : AggregateRoot<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SubjectId { get; set; }
    public int BlueprintVersionId { get; set; }
    public ExamStatus Status { get; set; }
    public string? Password { get; set; }
    public int DurationMin { get; set; }
    public ResultVisibilityTiming ResultVisibilityTiming { get; set; }
    public bool ShowTotalScore { get; set; }
    public bool ShowCorrectAnswers { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public int VariantCount { get; set; }

    public Subject Subject { get; set; } = default!;
    public ExamBlueprintVersion BlueprintVersion { get; set; } = default!;
    public ICollection<ExamVariant> Variants { get; set; } = new List<ExamVariant>();
}
