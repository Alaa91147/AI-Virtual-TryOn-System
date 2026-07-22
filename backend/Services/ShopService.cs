using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Shop;

namespace VirtualTryOn.Api.Services;

public class ShopService(
    AppDbContext dbContext)
{
    public async Task<ShopHomeResponse?>
        GetHomeAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return null;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(account =>
                account.Id == userId.Value)
            .Select(account => new
            {
                account.Gender,
                account.ShoppingPreference
            })
            .SingleOrDefaultAsync(
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var shoppingPreference =
            ResolveShoppingPreference(
                user.Gender,
                user.ShoppingPreference);

        if (shoppingPreference is null)
        {
            return new ShopHomeResponse(
                ShoppingPreference: null,
                RequiresShoppingPreference: true,
                Categories: [],
                Products: []);
        }

        var allowedAudiences =
            GetAllowedAudiences(
                shoppingPreference);

        var categories =
            await dbContext.Categories
                .AsNoTracking()
                .Where(category =>
                    category.IsActive &&
                    allowedAudiences.Contains(
                        category.Audience))
                .OrderBy(category =>
                    category.Audience)
                .ThenBy(category =>
                    category.DisplayOrder)
                .Select(category =>
                    new ShopCategoryResponse(
                        category.Id,
                        category.Name,
                        category.Slug,
                        category.Audience,
                        category.ImageUrl,
                        category.DisplayOrder))
                .ToListAsync(
                    cancellationToken);

        var products =
            await dbContext.Products
                .AsNoTracking()
                .Where(product =>
                    product.IsActive &&
                    product.Category.IsActive &&
                    allowedAudiences.Contains(
                        product.Category.Audience))
                .OrderByDescending(product =>
                    product.IsNew)
                .ThenByDescending(product =>
                    product.Rating)
                .ThenBy(product =>
                    product.Name)
                .Take(24)
                .Select(product =>
                    new ShopProductResponse(
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
            userId.Value),
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
                            .ToList()))
                .ToListAsync(
                    cancellationToken);

        return new ShopHomeResponse(
            ShoppingPreference:
                shoppingPreference,
            RequiresShoppingPreference: false,
            Categories: categories,
            Products: products);
    }

    private static string?
        ResolveShoppingPreference(
            string gender,
            string? savedPreference)
    {
        var normalizedPreference =
            savedPreference?
                .Trim()
                .ToLowerInvariant();

        if (normalizedPreference is not null &&
            ShoppingPreferences.All.Contains(
                normalizedPreference))
        {
            return normalizedPreference;
        }

        var normalizedGender =
            gender.Trim().ToLowerInvariant();

        return normalizedGender switch
        {
            GenderOptions.Female =>
                ShoppingPreferences.Women,

            GenderOptions.Male =>
                ShoppingPreferences.Men,

            _ => null
        };
    }

    private static string[]
        GetAllowedAudiences(
            string shoppingPreference)
    {
        return shoppingPreference switch
        {
            ShoppingPreferences.Women =>
            [
                CatalogAudiences.Women,
                CatalogAudiences.Unisex
            ],

            ShoppingPreferences.Men =>
            [
                CatalogAudiences.Men,
                CatalogAudiences.Unisex
            ],

            ShoppingPreferences.Both =>
            [
                CatalogAudiences.Women,
                CatalogAudiences.Men,
                CatalogAudiences.Unisex
            ],

            _ => []
        };
    }

    private static Guid?
        GetUserId(
            ClaimsPrincipal principal)
    {
        var userIdValue =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            userIdValue,
            out var userId)
            ? userId
            : null;
    }
}