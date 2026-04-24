using MTCA.Domain.Common;
using MTCA.Domain.Identity;

namespace MTCA.Domain.Exams;

public class ExamVariant : BaseEntity<int>
{
    public int ExamId { get; set; }
    public int VariantNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedById { get; set; }

    public Exam Exam { get; set; } = default!;
    public ApplicationUser CreatedBy { get; set; } = default!;
    public ICollection<ExamVariantQuestion> Questions { get; set; } = new List<ExamVariantQuestion>();
}
