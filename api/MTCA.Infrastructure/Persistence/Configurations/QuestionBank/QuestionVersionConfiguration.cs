using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.QuestionBank;

namespace MTCA.Infrastructure.Persistence.Configurations.QuestionBank;

public class QuestionVersionConfiguration : IEntityTypeConfiguration<QuestionVersion>
{
    public void Configure(EntityTypeBuilder<QuestionVersion> b)
    {
        b.ToTable("QuestionVersion");
        b.HasKey(x => x.Id);

        b.Property(x => x.BodyLatex).IsRequired();
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.CreatedById).IsRequired().HasMaxLength(450);

        b.HasIndex(x => new { x.QuestionId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UQ_QuestionVersion_Question_Version");

        b.HasOne(x => x.Question)
            .WithMany(q => q.Versions)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
