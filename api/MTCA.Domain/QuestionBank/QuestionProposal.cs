using MTCA.Domain.Common;
using MTCA.Domain.Identity;
using MTCA.Domain.QuestionBank.Enums;

namespace MTCA.Domain.QuestionBank;

public class QuestionProposal : AggregateRoot<int>
{
    public int QuestionId { get; set; }
    public Guid? ReviewerId { get; set; }
    public ProposalStatus Status { get; set; }
    public string? Reason { get; set; }

    public Question Question { get; set; } = default!;
    public ApplicationUser? Reviewer { get; set; }
}
