namespace VirtualTryOn.Api.Models;

public class Favorite
{
    public Guid UserId { get; set; }

    public Guid ProductId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;

    public Product Product { get; set; } = null!;
}