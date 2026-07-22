using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Cart;

public sealed class AddCartItemRequest
{
    [Required]
    public Guid ProductSizeId { get; set; }

    [Range(
        1,
        20,
        ErrorMessage = "Quantity must be between 1 and 20.")]
    public int Quantity { get; set; } = 1;
}

public sealed class UpdateCartItemRequest
{
    [Range(
        1,
        20,
        ErrorMessage = "Quantity must be between 1 and 20.")]
    public int Quantity { get; set; }
}

public sealed record CartItemResponse(
    Guid Id,
    Guid ProductId,
    Guid ProductSizeId,
    string ProductName,
    string ProductSlug,
    string CategoryName,
    string Audience,
    string ImageUrl,
    string Size,
    decimal UnitPrice,
    int Quantity,
    int StockQuantity,
    decimal LineTotal);

public sealed record CartResponse(
    IReadOnlyList<CartItemResponse> Items,
    int TotalQuantity,
    decimal Subtotal);