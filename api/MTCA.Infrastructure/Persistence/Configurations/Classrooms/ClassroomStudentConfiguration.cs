using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Classrooms;

namespace MTCA.Infrastructure.Persistence.Configurations.Classrooms;

public class ClassroomStudentConfiguration : IEntityTypeConfiguration<ClassroomStudent>
{
    public void Configure(EntityTypeBuilder<ClassroomStudent> b)
    {
        b.ToTable("ClassroomStudent");
        b.HasKey(x => x.Id);

        b.Property(x => x.StudentUserId).IsRequired().HasMaxLength(450);
        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.ClassroomId, x.StudentUserId })
            .IsUnique()
            .HasDatabaseName("UQ_ClassroomStudent_Classroom_Student");
        b.HasIndex(x => x.StudentUserId).HasDatabaseName("IX_ClassroomStudent_StudentUserId");

        b.HasOne(x => x.Classroom)
            .WithMany(c => c.Students)
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
