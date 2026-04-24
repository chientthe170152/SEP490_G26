using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.ExamSessions;

namespace MTCA.Infrastructure.Persistence.Configurations.ExamSessions;

public class SubmissionItemConfiguration : IEntityTypeConfiguration<SubmissionItem>
{
    public void Configure(EntityTypeBuilder<SubmissionItem> b)
    {
        b.ToTable("SubmissionItem");
        b.HasKey(x => x.Id);

        b.Property(x => x.RawAnswer).IsRequired();
        b.Property(x => x.Score).HasPrecision(5, 2);
        b.Property(x => x.MaxScore).HasPrecision(5, 2);

        b.HasIndex(x => new { x.SubmissionId, x.ExamVariantQuestionId })
            .IsUnique()
            .HasDatabaseName("UQ_SubmissionItem_Submission_Question");
        b.HasIndex(x => x.SubmissionId).HasDatabaseName("IX_SubmissionItem_SubmissionId");

        b.HasOne(x => x.Submission)
            .WithMany(s => s.Items)
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ExamVariantQuestion)
            .WithMany()
            .HasForeignKey(x => x.ExamVariantQuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
