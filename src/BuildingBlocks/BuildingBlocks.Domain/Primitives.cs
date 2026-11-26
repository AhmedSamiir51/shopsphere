using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.BuildingBlocks.Domain;

/// <summary>A domain event: something that happened inside the domain model that
/// other parts of the same service may react to. Never leaves the process.</summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOnUtc { get; }
}

/// <summary>Base class for all entities. Identity is by <see cref="Id"/>, not by values.</summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>Domain events raised by this entity, dispatched after persistence.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>Marks the consistency boundary of an aggregate. All invariants are
/// enforced here; outside code can only reference the root.</summary>
public abstract class AggregateRoot : Entity
{
}

/// <summary>An immutable, identity-less concept (Money, Address...). Equal when all
/// equality components are equal.</summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj) =>
        obj is ValueObject other &&
        GetType() == other.GetType() &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(17, (hash, c) => hash * 31 + (c?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}

/// <summary>Thrown when a business rule is violated. Mapped to HTTP 400 by the
/// global exception handler.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

/// <summary>Simple base record for domain events.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
