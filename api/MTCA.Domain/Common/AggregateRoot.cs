using System.ComponentModel.DataAnnotations.Schema;

namespace MTCA.Domain.Common;

public abstract class AggregateRoot<TKey> : AuditableEntity<TKey>
{
    private readonly List<IDomainEvent> _events = new();

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected void Raise(IDomainEvent @event) => _events.Add(@event);

    public void ClearDomainEvents() => _events.Clear();
}
