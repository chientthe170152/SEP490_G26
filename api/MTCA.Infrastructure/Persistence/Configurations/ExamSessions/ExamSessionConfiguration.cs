using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.ExamSessions;
using MTCA.Domain.ExamSessions.Enums;
using MTCA.Domain.Identity;

namespace MTCA.Infrastructure.Persistence.Configurations.ExamSessions;

public class ExamSessionConfiguration : IEntityTypeConfiguration<ExamSession>
{
    public void Configure(EntityTypeBuilder<ExamSession> b)
    {
        b.ToTable("ExamSession", t => t.HasCheckConstraint(
            "CK_ExamSession_MinDurationPolicy",
            "([EarlySubmitPolicy] = 2 AND [MinDurationMinutes] IS NOT NULL) OR ([EarlySubmitPolicy] <> 2 AND [MinDurationMinutes] IS NULL)"));
        b.HasKey(x => x.Id);

        b.Property(x => x.Status).HasConversion<byte>().HasDefaultValue(ExamSessionStatus.SCHEDULED);
        b.Property(x => x.EarlySubmitPolicy).HasConversion<byte>().HasDefaultValue(EarlySubmitPolicy.ALLOW);

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.ExamId, x.ClassroomId })
            .IsUnique()
            .HasDatabaseName("UQ_ExamSession_Exam_Classroom");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_ExamSession_Status");
        b.HasIndex(x => x.ClassroomId).HasDatabaseName("IX_ExamSession_ClassroomId");

        b.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CancelledBy)
            .WithMany()
            .HasForeignKey(x => x.CancelledById)
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
