using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.MasterData;

namespace MTCA.Infrastructure.Persistence.Configurations.MasterData;

public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> b)
    {
        b.ToTable("Chapter");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(255);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasOne(x => x.Subject)
            .WithMany(s => s.Chapters)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.SubjectId, x.OrderIndex })
            .IsUnique()
            .HasDatabaseName("UQ_Chapter_Subject_Order");
    }
}
