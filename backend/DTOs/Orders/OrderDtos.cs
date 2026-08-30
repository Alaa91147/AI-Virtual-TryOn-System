using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Orders;

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    Guid? ProductVariantId,
    string ProductName,
    string ImageUrl,
    string SizeName,
    string? ColorName,
    string? Sku,
    decimal OriginalUnitPrice,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    string CustomerName,
    string CustomerEmail,
    string Status,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal Total,
    string? TrackingNumber,
    string? ShippingCarrier,
    string? FulfillmentNotes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public sealed class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(80)]
    public string? ShippingCarrier { get; set; }

    [MaxLength(1000)]
    public string? FulfillmentNotes { get; set; }
}
