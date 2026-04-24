using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTCA.Domain.Logging;

namespace MTCA.Infrastructure.Persistence.Configurations.Logging;

public class EventLogConfiguration : IEntityTypeConfiguration<EventLog>
{
    public void Configure(EntityTypeBuilder<EventLog> b)
    {
        b.ToTable("EventLog");
        b.HasKey(x => x.Id);

        b.Property(x => x.Timestamp).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.Type).HasConversion<byte>();
        b.Property(x => x.DetailsJson).IsRequired();

        b.HasIndex(x => new { x.Type, x.Timestamp }).HasDatabaseName("IX_EventLog_Type_Timestamp");
        b.HasIndex(x => new { x.ActorId, x.Timestamp }).HasDatabaseName("IX_EventLog_Actor_Timestamp");

        b.HasOne(x => x.Actor)
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
