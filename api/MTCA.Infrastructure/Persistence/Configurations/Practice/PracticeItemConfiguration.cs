using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Practice;

namespace MTCA.Infrastructure.Persistence.Configurations.Practice;

public class PracticeItemConfiguration : IEntityTypeConfiguration<PracticeItem>
{
    public void Configure(EntityTypeBuilder<PracticeItem> b)
    {
        b.ToTable("PracticeItem");
        b.HasKey(x => x.Id);

        b.HasIndex(x => new { x.PracticeSessionId, x.AnsweredAt })
            .HasDatabaseName("IX_PracticeItem_Session_Answered");

        b.HasOne(x => x.PracticeSession)
            .WithMany(s => s.Items)
            .HasForeignKey(x => x.PracticeSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.QuestionVersion)
            .WithMany()
            .HasForeignKey(x => x.QuestionVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
