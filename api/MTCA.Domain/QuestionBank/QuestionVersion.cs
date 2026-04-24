using MTCA.Domain.Identity;

namespace MTCA.Domain.QuestionBank;

public class QuestionVersion
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public int VersionNumber { get; set; }
    public string BodyLatex { get; set; } = default!;
    public string? BodyMathJson { get; set; }
    public string? TemplateVarsJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedById { get; set; } = default!;

    public Question Question { get; set; } = default!;
    public ApplicationUser CreatedBy { get; set; } = default!;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<QuestionBlank> Blanks { get; set; } = new List<QuestionBlank>();
}
