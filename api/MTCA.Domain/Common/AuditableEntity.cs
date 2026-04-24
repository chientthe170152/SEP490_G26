namespace MTCA.Domain.Common;

public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedById { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedById { get; set; }
    public byte[] RowVersion { get; set; } = default!;
}
