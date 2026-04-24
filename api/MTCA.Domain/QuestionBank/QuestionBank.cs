using MTCA.Domain.Common;
using MTCA.Domain.Identity;
using MTCA.Domain.MasterData;
using MTCA.Domain.QuestionBank.Enums;

namespace MTCA.Domain.QuestionBank;

public class QuestionBank : AggregateRoot<int>
{
    public int SubjectId { get; set; }
    public Guid? OwnerId { get; set; }
    public QuestionBankPurpose Purpose { get; set; }
    public string Name { get; set; } = default!;

    public Subject Subject { get; set; } = default!;
    public ApplicationUser? Owner { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
