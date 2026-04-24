using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.QuestionBank;

namespace MTCA.Infrastructure.Persistence.Configurations.QuestionBank;

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> b)
    {
        b.ToTable("QuestionOption");
        b.HasKey(x => x.Id);

        b.Property(x => x.ContentLatex).IsRequired();

        b.HasIndex(x => new { x.QuestionVersionId, x.OrderIndex })
            .IsUnique()
            .HasDatabaseName("UQ_QuestionOption_Version_Order");

        b.HasOne(x => x.QuestionVersion)
            .WithMany(v => v.Options)
            .HasForeignKey(x => x.QuestionVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
