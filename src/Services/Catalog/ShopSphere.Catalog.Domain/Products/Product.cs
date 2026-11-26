using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.Catalog.Domain.Products;

public sealed class Category : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    private Category() { }
    public static Category Create(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Category name is required.");
        return new Category { Name = name.Trim(), Slug = slug.Trim().ToLowerInvariant() };
    }
}

public sealed record ProductPriceChanged(Guid ProductId, decimal OldPrice, decimal NewPrice) : DomainEvent;
public sealed record ProductAdded(Guid ProductId) : DomainEvent;

public sealed class Product : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public string ImageUrl { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public double Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Product() { }

    public static Product Create(string name, string sku, string description, decimal price, string currency,
        string imageUrl, Guid categoryId, double rating = 0, int reviewCount = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU is required.");
        if (price <= 0) throw new DomainException("Price must be positive.");
        var p = new Product
        {
            Name = name.Trim(), Sku = sku.Trim().ToUpperInvariant(), Description = description,
            Price = decimal.Round(price, 2), Currency = currency.ToUpperInvariant(), ImageUrl = imageUrl,
            CategoryId = categoryId, Rating = rating, ReviewCount = reviewCount
        };
        p.Raise(new ProductAdded(p.Id));
        return p;
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new DomainException("Price must be positive.");
        newPrice = decimal.Round(newPrice, 2);
        if (newPrice == Price) return;
        var old = Price;
        Price = newPrice;
        Raise(new ProductPriceChanged(Id, old, newPrice));
    }

    public void Discontinue() => IsActive = false;
}
