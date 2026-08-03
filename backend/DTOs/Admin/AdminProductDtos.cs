using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Admin;

public sealed record AdminCatalogResponse(
    IReadOnlyList<AdminCategoryResponse> Categories,
    IReadOnlyList<AdminProductResponse> Products);

public sealed record AdminCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string Audience);

public sealed record AdminProductResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Audience,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string ImageUrl,
    string? Badge,
    bool IsNew,
    bool IsActive,
    IReadOnlyList<AdminProductColorResponse> Colors,
    IReadOnlyList<AdminProductSizeResponse> Sizes);

public sealed record AdminProductColorResponse(
    Guid Id,
    string Name,
    string HexCode);

public sealed record AdminProductSizeResponse(
    Guid Id,
    string Name,
    int StockQuantity);

public sealed class SaveAdminProductRequest
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 99999999)]
    public decimal Price { get; set; }

    [Required, MaxLength(2048)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Badge { get; set; }

    public bool IsActive { get; set; } = true;

    public List<SaveAdminProductColorRequest> Colors { get; set; } = [];

    public List<SaveAdminProductSizeRequest> Sizes { get; set; } = [];
}

public sealed class SaveAdminProductColorRequest
{
    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(9)]
    public string HexCode { get; set; } = string.Empty;
}

public sealed class SaveAdminProductSizeRequest
{
    [Required, MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}
