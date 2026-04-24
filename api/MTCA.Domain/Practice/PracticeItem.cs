using MTCA.Domain.Common;
using MTCA.Domain.QuestionBank;

namespace MTCA.Domain.Practice;

public class PracticeItem : BaseEntity<int>
{
    public int PracticeSessionId { get; set; }
    public int QuestionVersionId { get; set; }
    public string? RawAnswer { get; set; }
    public string? ResolvedVarsJson { get; set; }
    public bool IsCorrect { get; set; }
    public string? PrtFeedbackJson { get; set; }
    public DateTime AnsweredAt { get; set; }

    public PracticeSession PracticeSession { get; set; } = default!;
    public QuestionVersion QuestionVersion { get; set; } = default!;
}
