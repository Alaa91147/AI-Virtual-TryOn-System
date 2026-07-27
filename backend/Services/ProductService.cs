using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Shop;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public class ProductService(
    AppDbContext dbContext)
{
    public async Task<ShopProductResponse?>
        GetProductAsync(
            ClaimsPrincipal principal,
            Guid productId,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return null;
        }

        var product =
            await ProductQuery()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == productId &&
                        item.IsActive,
                    cancellationToken);

        if (product is null)
        {
            return null;
        }

        await RecordViewAsync(
            userId.Value,
            productId,
            cancellationToken);

        return ToResponse(
            product,
            userId.Value);
    }

    public async Task<
        IReadOnlyList<ShopProductResponse>>
        GetRecentlyViewedAsync(
            ClaimsPrincipal principal,
            int limit,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return [];
        }

        var safeLimit =
            Math.Clamp(limit, 1, 30);

        var productIds =
            await dbContext
                .RecentlyViewedProducts
                .AsNoTracking()
                .Where(view =>
                    view.UserId ==
                        userId.Value)
                .OrderByDescending(view =>
                    view.ViewedAt)
                .Take(safeLimit)
                .Select(view =>
                    view.ProductId)
                .ToListAsync(
                    cancellationToken);

        if (productIds.Count == 0)
        {
            return [];
        }

        var products =
            await ProductQuery()
                .Where(product =>
                    productIds.Contains(
                        product.Id) &&
                    product.IsActive)
                .ToListAsync(
                    cancellationToken);

        var productsById =
            products.ToDictionary(
                product => product.Id);

        return productIds
            .Where(productsById.ContainsKey)
            .Select(productId =>
                ToResponse(
                    productsById[productId],
                    userId.Value))
            .ToList();
    }

    private async Task RecordViewAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var viewedProduct =
            await dbContext
                .RecentlyViewedProducts
                .SingleOrDefaultAsync(
                    view =>
                        view.UserId == userId &&
                        view.ProductId ==
                            productId,
                    cancellationToken);

        if (viewedProduct is null)
        {
            dbContext
                .RecentlyViewedProducts
                .Add(
                    new RecentlyViewedProduct
                    {
                        UserId = userId,
                        ProductId = productId,
                        ViewedAt =
                            DateTimeOffset.UtcNow
                    });
        }
        else
        {
            viewedProduct.ViewedAt =
                DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private IQueryable<Product>
        ProductQuery()
    {
        return dbContext.Products
            .AsNoTracking()
            .Include(product =>
                product.Category)
            .Include(product =>
                product.Colors)
            .Include(product =>
                product.Sizes)
            .Include(product =>
                product.Favorites);
    }

    private static ShopProductResponse
        ToResponse(
            Product product,
            Guid userId)
    {
        return new ShopProductResponse(
            product.Id,
            product.CategoryId,
            product.Category.Name,
            product.Category.Slug,
            product.Category.Audience,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.ImageUrl,
            product.Badge,
            product.Rating,
product.ReviewCount,
product.Favorites.Any(
    favorite =>
        favorite.UserId ==
            userId),
product.IsNew,
product.Colors
                .OrderBy(color =>
                    color.Name)
                .Select(color =>
                    new ShopProductColorResponse(
                        color.Id,
                        color.Name,
                        color.HexCode))
                .ToList(),
            product.Sizes
                .OrderBy(size =>
                    size.Name)
                .Select(size =>
                    new ShopProductSizeResponse(
                        size.Id,
                        size.Name,
                        size.StockQuantity))
                .ToList());
    }

    private static Guid?
        GetUserId(
            ClaimsPrincipal principal)
    {
        var value =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            value,
            out var userId)
            ? userId
            : null;
    }
}