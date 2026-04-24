using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Analytics;

namespace MTCA.Infrastructure.Persistence.Configurations.Analytics;

public class GeneratedVariantConfiguration : IEntityTypeConfiguration<GeneratedVariant>
{
    public void Configure(EntityTypeBuilder<GeneratedVariant> b)
    {
        b.ToTable("GeneratedVariant", t => t.HasCheckConstraint(
            "CK_GeneratedVariant_OneContext",
            "(CASE WHEN [SubmissionId] IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN [PracticeSessionId] IS NOT NULL THEN 1 ELSE 0 END) = 1"));

        b.HasKey(x => x.Id);

        b.Property(x => x.ResolvedVarsJson).IsRequired();
        b.Property(x => x.ResolvedAnswerJson).IsRequired();
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => x.QuestionVersionId).HasDatabaseName("IX_GeneratedVariant_QuestionVersionId");
        b.HasIndex(x => x.SubmissionId).HasDatabaseName("IX_GeneratedVariant_SubmissionId");
        b.HasIndex(x => x.PracticeSessionId).HasDatabaseName("IX_GeneratedVariant_PracticeSessionId");

        b.HasOne(x => x.QuestionVersion)
            .WithMany()
            .HasForeignKey(x => x.QuestionVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Submission)
            .WithMany()
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.PracticeSession)
            .WithMany()
            .HasForeignKey(x => x.PracticeSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
