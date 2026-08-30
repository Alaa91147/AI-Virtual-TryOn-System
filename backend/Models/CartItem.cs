namespace VirtualTryOn.Api.Models;

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid ProductSizeId { get; set; }

    public Guid? ProductColorId { get; set; }

    public Guid? ProductVariantId { get; set; }

    public int Quantity { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public ProductSize ProductSize { get; set; } = null!;

    public ProductColor? ProductColor { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}
