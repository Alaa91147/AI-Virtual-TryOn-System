namespace VirtualTryOn.Api.Models;

public class ProductSize
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<CartItem> CartItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
