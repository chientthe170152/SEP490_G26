using MTCA.Domain.Exams;

namespace MTCA.Domain.ExamSessions;

public class SubmissionItem
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int ExamVariantQuestionId { get; set; }
    public string RawAnswer { get; set; } = default!;
    public string? ResolvedVarsJson { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public string? PrtFeedbackJson { get; set; }
    public DateTime AnsweredAt { get; set; }

    public Submission Submission { get; set; } = default!;
    public ExamVariantQuestion ExamVariantQuestion { get; set; } = default!;
}
