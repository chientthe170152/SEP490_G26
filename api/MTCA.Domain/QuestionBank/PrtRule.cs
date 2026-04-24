using MTCA.Domain.QuestionBank.Enums;

namespace MTCA.Domain.QuestionBank;

public class PrtRule
{
    public int Id { get; set; }
    public int QuestionBlankId { get; set; }
    public int? ParentRuleId { get; set; }
    public int Priority { get; set; }
    public PrtConditionType ConditionType { get; set; }
    public string TargetExpression { get; set; } = default!;
    public string FeedbackText { get; set; } = default!;

    public QuestionBlank QuestionBlank { get; set; } = default!;
    public PrtRule? ParentRule { get; set; }
    public ICollection<PrtRule> ChildRules { get; set; } = new List<PrtRule>();
}
