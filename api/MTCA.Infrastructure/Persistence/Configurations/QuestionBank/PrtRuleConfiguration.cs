using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.QuestionBank;

namespace MTCA.Infrastructure.Persistence.Configurations.QuestionBank;

public class PrtRuleConfiguration : IEntityTypeConfiguration<PrtRule>
{
    public void Configure(EntityTypeBuilder<PrtRule> b)
    {
        b.ToTable("PrtRule");
        b.HasKey(x => x.Id);

        b.Property(x => x.ConditionType).HasConversion<byte>();
        b.Property(x => x.TargetExpression).IsRequired();
        b.Property(x => x.FeedbackText).IsRequired();

        b.HasIndex(x => new { x.QuestionBlankId, x.Priority }).HasDatabaseName("IX_PrtRule_Blank_Priority");
        b.HasIndex(x => x.ParentRuleId).HasDatabaseName("IX_PrtRule_ParentRuleId");

        b.HasOne(x => x.QuestionBlank)
            .WithMany(bk => bk.PrtRules)
            .HasForeignKey(x => x.QuestionBlankId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ParentRule)
            .WithMany(p => p.ChildRules)
            .HasForeignKey(x => x.ParentRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
