namespace VirtualTryOn.Api.Models;

public class Promotion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailSent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<PromotionProduct> Products { get; set; } = [];
    public ICollection<PromotionEmailDelivery> EmailDeliveries { get; set; } = [];
}

public class PromotionProduct
{
    public Guid PromotionId { get; set; }
    public Guid ProductId { get; set; }
    public Promotion Promotion { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class PromotionEmailDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PromotionId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
    public Promotion Promotion { get; set; } = null!;
    public User User { get; set; } = null!;
}
