using MTCA.Domain.Common;
using MTCA.Domain.QuestionBank;

namespace MTCA.Domain.Exams;

public class ExamVariantQuestion : BaseEntity<int>
{
    public int ExamVariantId { get; set; }
    public int QuestionVersionId { get; set; }
    public decimal Score { get; set; }

    public ExamVariant ExamVariant { get; set; } = default!;
    public QuestionVersion QuestionVersion { get; set; } = default!;
}
