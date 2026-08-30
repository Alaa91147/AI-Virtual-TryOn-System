namespace VirtualTryOn.Api.Models;

public sealed class InventoryAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid ProductSizeId { get; set; }
    public Guid AdminUserId { get; set; }
    public int PreviousQuantity { get; set; }
    public int NewQuantity { get; set; }
    public int Difference => NewQuantity - PreviousQuantity;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Product Product { get; set; } = null!;
    public ProductSize ProductSize { get; set; } = null!;
}
