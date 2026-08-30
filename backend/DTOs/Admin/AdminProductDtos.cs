using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Admin;

public sealed record AdminCatalogResponse(
    IReadOnlyList<AdminCategoryResponse> Categories,
    IReadOnlyList<AdminProductResponse> Products);

public sealed record AdminCategoryResponse(
    Guid Id, string Name, string Slug, string Audience);

public sealed record AdminProductResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Audience,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? SalePrice,
    decimal? DiscountPercentage,
    string ImageUrl,
    string OriginalImageUrl,
    string? AiMaskImageUrl,
    string? Badge,
    bool IsNew,
    bool IsActive,
    IReadOnlyList<AdminProductColorResponse> Colors,
    IReadOnlyList<AdminProductSizeResponse> Sizes);

public sealed record AdminProductColorResponse(
    Guid Id, string Name, string HexCode, string? ImageUrl);

public sealed record AdminProductSizeResponse(
    Guid Id, string Name, int StockQuantity);

public sealed class SaveAdminProductRequest : IValidatableObject
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(180), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Use lowercase letters, numbers, and single hyphens for the slug.")]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 99999999)]
    public decimal Price { get; set; }

    [Required, MaxLength(2048)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? OriginalImageUrl { get; set; }

    [MaxLength(2048)]
    public string? AiMaskImageUrl { get; set; }

    [MaxLength(30)]
    public string? Badge { get; set; }

    public bool IsActive { get; set; } = true;
    [MinLength(1)]
    public List<SaveAdminProductColorRequest> Colors { get; set; } = [];

    [MinLength(1)]
    public List<SaveAdminProductSizeRequest> Sizes { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Colors.GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
            yield return new ValidationResult("Color names must be unique.", [nameof(Colors)]);

        if (Sizes.GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
            yield return new ValidationResult("Size names must be unique.", [nameof(Sizes)]);
    }
}

public sealed class SaveAdminProductColorRequest
{
    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(9), RegularExpression("^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$",
        ErrorMessage = "Use a valid hexadecimal color such as #171717.")]
    public string HexCode { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ImageUrl { get; set; }
}

public sealed class SaveAdminProductSizeRequest
{
    [Required, MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}
