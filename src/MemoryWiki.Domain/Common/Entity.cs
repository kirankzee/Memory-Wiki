namespace MemoryWiki.Domain.Common;

/// <summary>Marker for domain events raised by aggregates.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>Base entity with identity equality.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>Aggregate root that collects domain events for dispatch.</summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
