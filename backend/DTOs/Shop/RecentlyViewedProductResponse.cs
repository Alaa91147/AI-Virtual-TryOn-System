namespace VirtualTryOn.Api.DTOs.Shop;

public sealed record RecentlyViewedProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string ImageUrl,
    decimal Price,
    string CategoryName,
    string CategorySlug,
    string Audience,
    DateTimeOffset ViewedAt);