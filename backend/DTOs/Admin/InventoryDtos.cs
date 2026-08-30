using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Admin;

public sealed class AdjustInventoryRequest
{
    [Range(0, int.MaxValue)] public int NewQuantity { get; set; }
    [Required, MaxLength(300)] public string Reason { get; set; } = string.Empty;
}

public sealed record InventoryAdjustmentResponse(
    Guid Id, Guid ProductId, string ProductName, Guid ProductSizeId,
    string SizeName, int PreviousQuantity, int NewQuantity, int Difference,
    string Reason, Guid AdminUserId, DateTimeOffset CreatedAt);
