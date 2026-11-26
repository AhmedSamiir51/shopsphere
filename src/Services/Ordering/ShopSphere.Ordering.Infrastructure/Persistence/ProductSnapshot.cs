namespace ShopSphere.Ordering.Infrastructure.Persistence;

/// <summary>Row of Ordering's local copy of Catalog prices.</summary>
public sealed class ProductSnapshot
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EGP";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
}
