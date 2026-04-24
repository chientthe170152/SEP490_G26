using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.QuestionBank;

namespace MTCA.Infrastructure.Persistence.Configurations.QuestionBank;

public class QuestionBlankConfiguration : IEntityTypeConfiguration<QuestionBlank>
{
    public void Configure(EntityTypeBuilder<QuestionBlank> b)
    {
        b.ToTable("QuestionBlank");
        b.HasKey(x => x.Id);

        b.Property(x => x.TargetExpression).IsRequired();

        b.HasIndex(x => new { x.QuestionVersionId, x.BlankIndex })
            .IsUnique()
            .HasDatabaseName("UQ_QuestionBlank_Version_Index");

        b.HasOne(x => x.QuestionVersion)
            .WithMany(v => v.Blanks)
            .HasForeignKey(x => x.QuestionVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
