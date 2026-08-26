using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Orders;

public sealed record OrderItemResponse(
    Guid Id, Guid ProductId, string ProductName, string ImageUrl,
    string SizeName, string? ColorName, decimal OriginalUnitPrice,
    decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderResponse(
    Guid Id, string OrderNumber, Guid UserId, string CustomerName,
    string CustomerEmail, string Status, decimal Subtotal,
    decimal DeliveryFee, decimal Total, string? TrackingNumber,
    DateTimeOffset CreatedAt, IReadOnlyList<OrderItemResponse> Items);

public sealed class UpdateOrderStatusRequest
{
    [Required] public string Status { get; set; } = string.Empty;
    [MaxLength(100)] public string? TrackingNumber { get; set; }
}
