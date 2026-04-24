using MTCA.Domain.Identity;
using MTCA.Domain.MasterData;

namespace MTCA.Domain.Practice;

public class PracticeSession
{
    public int Id { get; set; }
    public string StudentUserId { get; set; } = default!;
    public int SubjectId { get; set; }
    public int SemesterId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public decimal? TotalScore { get; set; }
    public int? TotalQuestions { get; set; }
    public string? ConfigJson { get; set; }

    public ApplicationUser Student { get; set; } = default!;
    public Subject Subject { get; set; } = default!;
    public Semester Semester { get; set; } = default!;
    public ICollection<PracticeItem> Items { get; set; } = new List<PracticeItem>();
}
