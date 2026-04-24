using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Exams;

namespace MTCA.Infrastructure.Persistence.Configurations.Exams;

public class ExamVariantConfiguration : IEntityTypeConfiguration<ExamVariant>
{
    public void Configure(EntityTypeBuilder<ExamVariant> b)
    {
        b.ToTable("ExamVariant");
        b.HasKey(x => x.Id);

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.CreatedById).IsRequired().HasMaxLength(450);

        b.HasIndex(x => new { x.ExamId, x.VariantNumber })
            .IsUnique()
            .HasDatabaseName("UQ_ExamVariant_Exam_Number");
        b.HasIndex(x => x.ExamId).HasDatabaseName("IX_ExamVariant_ExamId");

        b.HasOne(x => x.Exam)
            .WithMany(e => e.Variants)
            .HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
