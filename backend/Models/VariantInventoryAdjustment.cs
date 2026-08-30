namespace VirtualTryOn.Api.Models;

public sealed class VariantInventoryAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductVariantId { get; set; }

    public Guid AdminUserId { get; set; }

    public string Operation { get; set; } = string.Empty;

    public int EnteredQuantity { get; set; }

    public int PreviousQuantity { get; set; }

    public int NewQuantity { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public int Difference =>
        NewQuantity - PreviousQuantity;

    public ProductVariant ProductVariant { get; set; } = null!;

    public User AdminUser { get; set; } = null!;
}
