using MTCA.Domain.Classrooms;
using MTCA.Domain.Common;
using MTCA.Domain.ExamSessions.Enums;
using MTCA.Domain.Exams;
using MTCA.Domain.Identity;

namespace MTCA.Domain.ExamSessions;

public class ExamSession : AggregateRoot<int>
{
    public int ExamId { get; set; }
    public int ClassroomId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int DurationMin { get; set; }
    public ExamSessionStatus Status { get; set; } = ExamSessionStatus.SCHEDULED;
    public string? CancelReason { get; set; }
    public Guid? CancelledById { get; set; }
    public DateTime? CancelledAt { get; set; }
    public long ShuffleSeed { get; set; }
    public EarlySubmitPolicy EarlySubmitPolicy { get; set; } = EarlySubmitPolicy.ALLOW;
    public int? MinDurationMinutes { get; set; }

    public Exam Exam { get; set; } = default!;
    public Classroom Classroom { get; set; } = default!;
    public ApplicationUser? CancelledBy { get; set; }
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
