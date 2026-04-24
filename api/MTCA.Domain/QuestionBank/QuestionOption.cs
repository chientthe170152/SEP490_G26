using MTCA.Domain.Common;

namespace MTCA.Domain.QuestionBank;

public class QuestionOption : BaseEntity<int>
{
    public int QuestionVersionId { get; set; }
    public int OrderIndex { get; set; }
    public string ContentLatex { get; set; } = default!;
    public string? ContentMathJson { get; set; }
    public bool IsCorrect { get; set; }

    public QuestionVersion QuestionVersion { get; set; } = default!;
}
