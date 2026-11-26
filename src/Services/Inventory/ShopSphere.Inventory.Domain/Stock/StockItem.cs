using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.Inventory.Domain.Stock;

/// <summary>Stock for one product. Reserved units are held for pending orders and
/// either committed (order confirmed) or released (order cancelled).</summary>
public sealed class StockItem : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public int Available { get; private set; }
    public int Reserved { get; private set; }

    private StockItem() { } // EF Core

    public static StockItem Create(Guid productId, int initialQuantity)
    {
        if (initialQuantity < 0) throw new DomainException("Initial stock cannot be negative.");
        return new StockItem { ProductId = productId, Available = initialQuantity };
    }

    public void Add(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        Available += quantity;
    }

    public bool CanReserve(int quantity) => quantity > 0 && Available >= quantity;

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity)) throw new DomainException($"Insufficient stock for product {ProductId}.");
        Available -= quantity;
        Reserved += quantity;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0 || quantity > Reserved) throw new DomainException("Invalid release quantity.");
        Reserved -= quantity;
        Available += quantity;
    }

    public void Commit(int quantity)
    {
        if (quantity <= 0 || quantity > Reserved) throw new DomainException("Invalid commit quantity.");
        Reserved -= quantity;
    }
}
