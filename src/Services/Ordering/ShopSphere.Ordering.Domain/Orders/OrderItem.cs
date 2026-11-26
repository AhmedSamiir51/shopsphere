using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Ordering.Domain.ValueObjects;

namespace ShopSphere.Ordering.Domain.Orders;

public sealed class OrderItem : Entity
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;

    private OrderItem() { } // EF Core

    internal OrderItem(Guid productId, string productName, int quantity, Money unitPrice)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be greater than zero.");
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Money LineTotal => UnitPrice.Multiply(Quantity);
}
