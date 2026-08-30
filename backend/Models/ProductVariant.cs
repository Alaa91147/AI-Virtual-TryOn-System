namespace VirtualTryOn.Api.Models;

public class ProductVariant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public Guid ProductColorId { get; set; }

    public Guid ProductSizeId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public int LowStockThreshold { get; set; } = 5;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public ProductColor ProductColor { get; set; } = null!;

    public ProductSize ProductSize { get; set; } = null!;

    public ICollection<VariantInventoryAdjustment> InventoryAdjustments { get; set; } = [];

    public ICollection<CartItem> CartItems { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}


