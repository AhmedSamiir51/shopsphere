using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.Cart.Domain.Carts;

public sealed record CartLine(Guid ProductId, string ProductName, decimal UnitPrice, string ImageUrl, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>A user's basket. Stored as one JSON document in Redis; rules live here, not in storage.</summary>
public sealed class ShoppingCart
{
    private readonly List<CartLine> _lines = new();

    public Guid UserId { get; private set; }
    public IReadOnlyList<CartLine> Lines => _lines;
    public decimal Total => _lines.Sum(l => l.LineTotal);

    public ShoppingCart(Guid userId, IEnumerable<CartLine>? lines = null)
    {
        UserId = userId;
        if (lines is not null) _lines.AddRange(lines);
    }

    public void AddOrIncrease(CartLine line)
    {
        if (line.Quantity <= 0) throw new DomainException("Quantity must be positive.");
        var i = _lines.FindIndex(l => l.ProductId == line.ProductId);
        if (i < 0) _lines.Add(line);
        else _lines[i] = _lines[i] with { Quantity = _lines[i].Quantity + line.Quantity, UnitPrice = line.UnitPrice };
    }

    public void SetQuantity(Guid productId, int quantity)
    {
        var i = _lines.FindIndex(l => l.ProductId == productId);
        if (i < 0) throw new DomainException("Item is not in the cart.");
        if (quantity <= 0) _lines.RemoveAt(i);
        else _lines[i] = _lines[i] with { Quantity = quantity };
    }

    public void Remove(Guid productId) => _lines.RemoveAll(l => l.ProductId == productId);
    public void Clear() => _lines.Clear();
}

public interface ICartRepository
{
    Task<ShoppingCart> GetAsync(Guid userId, CancellationToken ct);
    Task SaveAsync(ShoppingCart cart, CancellationToken ct);
    Task DeleteAsync(Guid userId, CancellationToken ct);
}
