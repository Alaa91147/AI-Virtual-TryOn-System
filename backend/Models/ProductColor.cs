namespace VirtualTryOn.Api.Models;

public class ProductColor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string HexCode { get; set; } = string.Empty;

    // Image showing the product in this specific color
    public string? ImageUrl { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<CartItem> CartItems { get; set; } = [];
}