using MTCA.Domain.Identity;

namespace MTCA.Domain.Exams;

public class ExamVariant
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int VariantNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedById { get; set; } = default!;

    public Exam Exam { get; set; } = default!;
    public ApplicationUser CreatedBy { get; set; } = default!;
    public ICollection<ExamVariantQuestion> Questions { get; set; } = new List<ExamVariantQuestion>();
}
