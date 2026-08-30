using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Admin;

public sealed record AdminInventoryVariantResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductImageUrl,
    string CategoryName,
    Guid ProductColorId,
    string ColorName,
    string ColorHexCode,
    string? ColorImageUrl,
    Guid ProductSizeId,
    string SizeName,
    string Sku,
    int StockQuantity,
    int LowStockThreshold,
    bool IsLowStock,
    bool IsActive);

public sealed class AdjustVariantInventoryRequest
{
    [Required]
    [RegularExpression(
        "^(Add|Remove|Set)$",
        ErrorMessage = "Operation must be Add, Remove, or Set.")]
    public string Operation { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Required, StringLength(300, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public sealed record VariantInventoryAdjustmentResponse(
    Guid Id,
    Guid ProductVariantId,
    Guid ProductId,
    string ProductName,
    string ProductImageUrl,
    string CategoryName,
    string ColorName,
    string ColorHexCode,
    string SizeName,
    string Sku,
    string Operation,
    int EnteredQuantity,
    int PreviousQuantity,
    int NewQuantity,
    int Difference,
    string Reason,
    Guid AdminUserId,
    string AdministratorName,
    string AdministratorEmail,
    DateTimeOffset CreatedAt);

public sealed record SynchronizeVariantsResponse(
    int CreatedCount,
    int ExistingCount,
    int TotalCount);
