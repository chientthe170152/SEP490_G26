using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.ExamBlueprints;
using MTCA.Domain.ExamBlueprints.Enums;
using MTCA.Domain.Identity;

namespace MTCA.Infrastructure.Persistence.Configurations.ExamBlueprints;

public class ExamBlueprintVersionConfiguration : IEntityTypeConfiguration<ExamBlueprintVersion>
{
    public void Configure(EntityTypeBuilder<ExamBlueprintVersion> b)
    {
        b.ToTable("ExamBlueprintVersion");
        b.HasKey(x => x.Id);

        b.Property(x => x.Status).HasConversion<byte>().HasDefaultValue(ExamBlueprintStatus.DRAFT);
        b.Property(x => x.TotalScore).HasPrecision(5, 2).HasDefaultValue(10.00m);

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.CreatedById).IsRequired().HasMaxLength(450);
        b.Property(x => x.UpdatedById).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.BlueprintId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UQ_ExamBlueprintVersion_Blueprint_Version");
        b.HasIndex(x => new { x.BlueprintId, x.Status })
            .HasDatabaseName("IX_ExamBlueprintVersion_Blueprint_Status");
        b.HasIndex(x => x.BlueprintId)
            .IsUnique()
            .HasFilter("[Status] = 1")
            .HasDatabaseName("UQ_ExamBlueprintVersion_Blueprint_Active");

        b.HasOne(x => x.Blueprint)
            .WithMany(bp => bp.Versions)
            .HasForeignKey(x => x.BlueprintId)
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
