using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Exams;
using MTCA.Domain.Identity;

namespace MTCA.Infrastructure.Persistence.Configurations.Exams;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> b)
    {
        b.ToTable("Exam");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Password).HasMaxLength(100);
        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.ResultVisibilityTiming).HasConversion<byte>();
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.SubjectId).HasDatabaseName("IX_Exam_SubjectId");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_Exam_Status");
        b.HasIndex(x => x.BlueprintVersionId).HasDatabaseName("IX_Exam_BlueprintVersionId");

        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.BlueprintVersion)
            .WithMany()
            .HasForeignKey(x => x.BlueprintVersionId)
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
