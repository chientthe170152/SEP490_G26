using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Identity;
using MTCA.Domain.Identity.Enums;

namespace MTCA.Infrastructure.Persistence.Configurations.Identity;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.ToTable("UserProfile");

        b.HasKey(p => p.UserId);
        b.Property(p => p.UserId).ValueGeneratedNever();

        b.Property(p => p.StudentCode).IsRequired().HasMaxLength(50);
        b.Property(p => p.FullName).IsRequired().HasMaxLength(255);
        b.Property(p => p.Status)
            .HasConversion<byte>()
            .HasDefaultValue(UserProfileStatus.ACTIVE);
        b.Property(p => p.Nickname).HasMaxLength(20);

        b.HasIndex(p => p.StudentCode).IsUnique();
        b.HasIndex(p => p.Nickname)
            .IsUnique()
            .HasFilter("[Nickname] IS NOT NULL")
            .HasDatabaseName("UQ_UserProfile_Nickname");

        b.HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(p => p.NicknameChangedInSemester)
            .WithMany()
            .HasForeignKey(p => p.NicknameChangedInSemesterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
