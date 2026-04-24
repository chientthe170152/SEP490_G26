using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Identity;
using MTCA.Domain.QuestionBank;

namespace MTCA.Infrastructure.Persistence.Configurations.QuestionBank;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> b)
    {
        b.ToTable("Question", t =>
        {
            t.HasCheckConstraint(
                "CK_Question_DifficultyExpected_Range",
                "[DifficultyExpected] BETWEEN 0 AND 1");
            t.HasCheckConstraint(
                "CK_Question_DifficultyEmpirical_Range",
                "[DifficultyEmpirical] BETWEEN 0 AND 1");
        });
        b.HasKey(x => x.Id);

        b.Property(x => x.BloomLevel).HasConversion<byte>();
        b.Property(x => x.DifficultyExpected).HasDefaultValue(0.5);
        b.Property(x => x.DifficultyEmpirical).HasDefaultValue(0.5);

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.CreatedById).IsRequired().HasMaxLength(450);
        b.Property(x => x.UpdatedById).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.SubjectId, x.BloomLevel }).HasDatabaseName("IX_Question_Subject_Bloom");
        b.HasIndex(x => x.ChapterId).HasDatabaseName("IX_Question_ChapterId");
        b.HasIndex(x => x.QuestionBankId).HasDatabaseName("IX_Question_QuestionBankId");

        b.HasOne(x => x.Bank)
            .WithMany(bk => bk.Questions)
            .HasForeignKey(x => x.QuestionBankId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Chapter).WithMany().HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CurrentVersion)
            .WithMany()
            .HasForeignKey(x => x.CurrentVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
