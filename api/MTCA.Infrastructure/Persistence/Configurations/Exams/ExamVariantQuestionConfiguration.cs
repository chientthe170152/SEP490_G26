using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Exams;

namespace MTCA.Infrastructure.Persistence.Configurations.Exams;

public class ExamVariantQuestionConfiguration : IEntityTypeConfiguration<ExamVariantQuestion>
{
    public void Configure(EntityTypeBuilder<ExamVariantQuestion> b)
    {
        b.ToTable("ExamVariantQuestion");
        b.HasKey(x => x.Id);

        b.Property(x => x.Score).HasPrecision(5, 2);

        b.HasIndex(x => new { x.ExamVariantId, x.QuestionVersionId })
            .IsUnique()
            .HasDatabaseName("UQ_ExamVariantQuestion_Variant_Version");
        b.HasIndex(x => x.QuestionVersionId).HasDatabaseName("IX_ExamVariantQuestion_QuestionVersionId");

        b.HasOne(x => x.ExamVariant)
            .WithMany(v => v.Questions)
            .HasForeignKey(x => x.ExamVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.QuestionVersion)
            .WithMany()
            .HasForeignKey(x => x.QuestionVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
