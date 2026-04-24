using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Practice;

namespace MTCA.Infrastructure.Persistence.Configurations.Practice;

public class PracticeSessionConfiguration : IEntityTypeConfiguration<PracticeSession>
{
    public void Configure(EntityTypeBuilder<PracticeSession> b)
    {
        b.ToTable("PracticeSession");
        b.HasKey(x => x.Id);

        b.Property(x => x.StudentUserId).IsRequired().HasMaxLength(450);
        b.Property(x => x.StartedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.TotalScore).HasPrecision(5, 2);

        b.HasIndex(x => new { x.StudentUserId, x.StartedAt })
            .HasDatabaseName("IX_PracticeSession_Student_Started");
        b.HasIndex(x => new { x.SubjectId, x.StartedAt })
            .HasDatabaseName("IX_PracticeSession_Subject_Started");

        b.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Semester).WithMany().HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.Restrict);
    }
}
