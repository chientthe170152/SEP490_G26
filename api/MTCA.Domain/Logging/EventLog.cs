using MTCA.Domain.Common;
using MTCA.Domain.Identity;
using MTCA.Domain.Logging.Enums;

namespace MTCA.Domain.Logging;

public class EventLog : BaseEntity<long>
{
    public DateTime Timestamp { get; set; }
    public EventLogType Type { get; set; }
    public Guid? ActorId { get; set; }
    public string DetailsJson { get; set; } = default!;

    public ApplicationUser? Actor { get; set; }
}
