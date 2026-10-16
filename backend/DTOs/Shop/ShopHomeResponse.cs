namespace VirtualTryOn.Api.DTOs.Shop;

public sealed record ShopHomeResponse(
    string? ShoppingPreference,
    bool RequiresShoppingPreference,
    IReadOnlyList<ShopCategoryResponse> Categories,
    IReadOnlyList<ShopProductResponse> Products);

public sealed record ShopCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string Audience,
    string? ImageUrl,
    int DisplayOrder);

public sealed record ShopProductResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    string Audience,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? SalePrice,
    decimal? DiscountPercentage,
    string ImageUrl,
    string? Badge,
    decimal Rating,
    int ReviewCount,
    bool IsFavorite,
    bool IsNew,
    IReadOnlyList<ShopProductColorResponse> Colors,
    IReadOnlyList<ShopProductSizeResponse> Sizes)
{
    public IReadOnlyList<ShopProductVariantResponse> Variants { get; init; }
        = Array.Empty<ShopProductVariantResponse>();
}

public sealed record ShopProductVariantResponse(
    Guid Id,
    Guid ProductSizeId,
    Guid ProductColorId,
    int StockQuantity);

public sealed record ShopProductColorResponse(
    Guid Id,
    string Name,
    string HexCode,
    string? ImageUrl);

public sealed record ShopProductSizeResponse(
    Guid Id,
    string Name,
    int StockQuantity);
