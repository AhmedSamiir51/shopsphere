using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.Inventory.Domain.Stock;

public enum ReservationStatus { Active = 0, Committed = 1, Released = 2 }

/// <summary>Records which units were held for an order, so compensation is exact and idempotent.</summary>
public sealed class Reservation : Entity
{
    public Guid OrderId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public List<ReservationLine> Lines { get; private set; } = new();

    private Reservation() { }

    public static Reservation Create(Guid orderId, IEnumerable<ReservationLine> lines) =>
        new() { OrderId = orderId, Status = ReservationStatus.Active, Lines = lines.ToList() };

    public void MarkCommitted()
    {
        if (Status != ReservationStatus.Active) throw new DomainException("Reservation is not active.");
        Status = ReservationStatus.Committed;
    }

    public void MarkReleased()
    {
        if (Status != ReservationStatus.Active) throw new DomainException("Reservation is not active.");
        Status = ReservationStatus.Released;
    }
}

public sealed record ReservationLine(Guid ProductId, int Quantity);
