using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Identity;

namespace MTCA.Infrastructure.Persistence.Configurations.QuestionBank;

public class QuestionBankConfiguration : IEntityTypeConfiguration<Domain.QuestionBank.QuestionBank>
{
    public void Configure(EntityTypeBuilder<Domain.QuestionBank.QuestionBank> b)
    {
        b.ToTable("QuestionBank");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Purpose).HasConversion<byte>();

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.OwnerId).HasDatabaseName("IX_QuestionBank_OwnerId");
        b.HasIndex(x => new { x.SubjectId, x.Purpose })
            .IsUnique()
            .HasFilter("[OwnerId] IS NULL")
            .HasDatabaseName("UQ_QuestionBank_Subject_Purpose_SubjectBank");

        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
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
