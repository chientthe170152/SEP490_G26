using MTCA.Domain.Identity;
using MTCA.Domain.Logging.Enums;

namespace MTCA.Domain.Logging;

public class EventLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public EventLogType Type { get; set; }
    public string? ActorId { get; set; }
    public string DetailsJson { get; set; } = default!;

    public ApplicationUser? Actor { get; set; }
}
