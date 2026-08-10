namespace VirtualTryOn.Api.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Untouched source used for every mask and recolor operation.
    public string OriginalImageUrl { get; set; } = string.Empty;

    // Customer-facing fallback image. It remains the original image.
    public string ImageUrl { get; set; } = string.Empty;
    public string? AiMaskImageUrl { get; set; }
    public string? Badge { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsNew { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<ProductColor> Colors { get; set; } = [];
    public ICollection<ProductSize> Sizes { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
    public ICollection<RecentlyViewedProduct> RecentlyViewedByUsers { get; set; } = [];
    public ICollection<ProductReview> Reviews { get; set; } = [];
}
