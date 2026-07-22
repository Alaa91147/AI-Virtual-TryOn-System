namespace VirtualTryOn.Api.Models;

public class RecentlyViewedProduct
{
    public Guid UserId { get; set; }

    public Guid ProductId { get; set; }

    public DateTimeOffset ViewedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;

    public Product Product { get; set; } = null!;
}