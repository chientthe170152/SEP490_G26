namespace MTCA.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
