using MTCA.Domain.ExamSessions;
using MTCA.Domain.Practice;
using MTCA.Domain.QuestionBank;

namespace MTCA.Domain.Analytics;

public class GeneratedVariant
{
    public int Id { get; set; }
    public int QuestionVersionId { get; set; }
    public int? SubmissionId { get; set; }
    public int? PracticeSessionId { get; set; }
    public long Seed { get; set; }
    public string ResolvedVarsJson { get; set; } = default!;
    public string ResolvedAnswerJson { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public QuestionVersion QuestionVersion { get; set; } = default!;
    public Submission? Submission { get; set; }
    public PracticeSession? PracticeSession { get; set; }
}
