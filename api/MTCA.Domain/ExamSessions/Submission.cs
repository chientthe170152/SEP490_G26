using MTCA.Domain.Common;
using MTCA.Domain.ExamSessions.Enums;
using MTCA.Domain.Exams;
using MTCA.Domain.Identity;

namespace MTCA.Domain.ExamSessions;

public class Submission : BaseEntity<int>, IConcurrencyAware
{
    public int ExamSessionId { get; set; }
    public Guid StudentUserId { get; set; }
    public int ExamVariantId { get; set; }
    public SubmissionStatus Status { get; set; }
    public DateTime TimerStartedAt { get; set; }
    public DateTime? TimerPausedAt { get; set; }
    public long TotalPausedMs { get; set; }
    public DateTime LastHeartbeatAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? TotalScore { get; set; }

    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = default!;

    public ExamSession ExamSession { get; set; } = default!;
    public ApplicationUser Student { get; set; } = default!;
    public ExamVariant ExamVariant { get; set; } = default!;
    public ICollection<SubmissionItem> Items { get; set; } = new List<SubmissionItem>();
}
