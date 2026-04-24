using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.ExamSessions;
using MTCA.Domain.ExamSessions.Enums;

namespace MTCA.Infrastructure.Persistence.Configurations.ExamSessions;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> b)
    {
        b.ToTable("Submission");
        b.HasKey(x => x.Id);

        b.Property(x => x.StudentUserId).IsRequired().HasMaxLength(450);
        b.Property(x => x.Status).HasConversion<byte>().HasDefaultValue(SubmissionStatus.IN_PROGRESS);
        b.Property(x => x.TotalPausedMs).HasDefaultValue(0L);
        b.Property(x => x.TotalScore).HasPrecision(5, 2);

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.ExamSessionId, x.StudentUserId })
            .IsUnique()
            .HasDatabaseName("UQ_Submission_Session_Student");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_Submission_Status");
        b.HasIndex(x => x.StudentUserId).HasDatabaseName("IX_Submission_StudentUserId");

        b.HasOne(x => x.ExamSession)
            .WithMany(s => s.Submissions)
            .HasForeignKey(x => x.ExamSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ExamVariant)
            .WithMany()
            .HasForeignKey(x => x.ExamVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
