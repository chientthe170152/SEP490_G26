using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.ExamBlueprints;

namespace MTCA.Infrastructure.Persistence.Configurations.ExamBlueprints;

public class BlueprintCellConfiguration : IEntityTypeConfiguration<BlueprintCell>
{
    public void Configure(EntityTypeBuilder<BlueprintCell> b)
    {
        b.ToTable("BlueprintCell");
        b.HasKey(x => x.Id);

        b.Property(x => x.BloomLevel).HasConversion<byte>();
        b.Property(x => x.ScorePerQuestion).HasPrecision(5, 2);

        b.HasIndex(x => new { x.BlueprintVersionId, x.ChapterId, x.BloomLevel })
            .IsUnique()
            .HasDatabaseName("UQ_BlueprintCell_Version_Chapter_Bloom");

        b.HasOne(x => x.BlueprintVersion)
            .WithMany(v => v.Cells)
            .HasForeignKey(x => x.BlueprintVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Chapter)
            .WithMany()
            .HasForeignKey(x => x.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
