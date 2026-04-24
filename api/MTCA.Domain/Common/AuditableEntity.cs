namespace MTCA.Domain.Common;

public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, IAuditable, IConcurrencyAware
{
    public DateTime CreatedAt { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
    public byte[] RowVersion { get; set; } = default!;
}
