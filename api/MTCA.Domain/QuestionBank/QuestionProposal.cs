using MTCA.Domain.Common;
using MTCA.Domain.Identity;
using MTCA.Domain.QuestionBank.Enums;

namespace MTCA.Domain.QuestionBank;

public class QuestionProposal : AuditableEntity
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string? ReviewerId { get; set; }
    public ProposalStatus Status { get; set; } = ProposalStatus.PENDING;
    public string? Reason { get; set; }

    public Question Question { get; set; } = default!;
    public ApplicationUser? Reviewer { get; set; }
}
