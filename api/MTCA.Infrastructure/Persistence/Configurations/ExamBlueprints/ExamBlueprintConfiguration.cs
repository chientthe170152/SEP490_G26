using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.ExamBlueprints;
using MTCA.Domain.Identity;

namespace MTCA.Infrastructure.Persistence.Configurations.ExamBlueprints;

public class ExamBlueprintConfiguration : IEntityTypeConfiguration<ExamBlueprint>
{
    public void Configure(EntityTypeBuilder<ExamBlueprint> b)
    {
        b.ToTable("ExamBlueprint");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.OwnerId).HasMaxLength(450);

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.CreatedById).IsRequired().HasMaxLength(450);
        b.Property(x => x.UpdatedById).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.SubjectId).HasDatabaseName("IX_ExamBlueprint_SubjectId");
        b.HasIndex(x => x.OwnerId).HasDatabaseName("IX_ExamBlueprint_OwnerId");
        b.HasIndex(x => new { x.SubjectId, x.OwnerId, x.Name }).HasDatabaseName("IX_ExamBlueprint_Identity");

        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CurrentVersion)
            .WithMany()
            .HasForeignKey(x => x.CurrentVersionId)
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
