using MTCA.Domain.Common;
using MTCA.Domain.MasterData;
using MTCA.Domain.QuestionBank.Enums;

namespace MTCA.Domain.QuestionBank;

public class Question : AuditableEntity
{
    public int Id { get; set; }
    public int QuestionBankId { get; set; }
    public int SubjectId { get; set; }
    public int ChapterId { get; set; }
    public int? CurrentVersionId { get; set; }
    public BloomLevel BloomLevel { get; set; }
    public double DifficultyExpected { get; set; } = 0.5;
    public double DifficultyEmpirical { get; set; } = 0.5;

    public QuestionBank Bank { get; set; } = default!;
    public Subject Subject { get; set; } = default!;
    public Chapter Chapter { get; set; } = default!;
    public QuestionVersion? CurrentVersion { get; set; }
    public ICollection<QuestionVersion> Versions { get; set; } = new List<QuestionVersion>();
}
