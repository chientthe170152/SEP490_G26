using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Identity;
using MTCA.Domain.QuestionBank;
using MTCA.Domain.QuestionBank.Enums;

namespace MTCA.Infrastructure.Persistence.Configurations.QuestionBank;

public class QuestionProposalConfiguration : IEntityTypeConfiguration<QuestionProposal>
{
    public void Configure(EntityTypeBuilder<QuestionProposal> b)
    {
        b.ToTable("QuestionProposal");
        b.HasKey(x => x.Id);

        b.Property(x => x.ReviewerId).HasMaxLength(450);
        b.Property(x => x.Status).HasConversion<byte>().HasDefaultValue(ProposalStatus.PENDING);

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.CreatedById).IsRequired().HasMaxLength(450);
        b.Property(x => x.UpdatedById).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.Status).HasDatabaseName("IX_QuestionProposal_Status");
        b.HasIndex(x => x.ReviewerId).HasDatabaseName("IX_QuestionProposal_ReviewerId");

        b.HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Reviewer)
            .WithMany()
            .HasForeignKey(x => x.ReviewerId)
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
