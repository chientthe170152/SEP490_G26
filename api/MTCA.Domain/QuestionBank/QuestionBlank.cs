namespace MTCA.Domain.QuestionBank;

public class QuestionBlank
{
    public int Id { get; set; }
    public int QuestionVersionId { get; set; }
    public int BlankIndex { get; set; }
    public int Position { get; set; }
    public string TargetExpression { get; set; } = default!;
    public string? TargetMathJson { get; set; }

    public QuestionVersion QuestionVersion { get; set; } = default!;
    public ICollection<PrtRule> PrtRules { get; set; } = new List<PrtRule>();
}
