using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Cart;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public enum CartMutationStatus
{
    Success,
    Unauthorized,
    NotFound,
    OutOfStock
}

public sealed record CartMutationResult(
    CartMutationStatus Status,
    CartResponse? Cart = null);

public class CartService(
    AppDbContext dbContext,
    NotificationService notificationService)
{
    public async Task<CartResponse?> GetAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null ||
            !await UserExistsAsync(
                userId.Value,
                cancellationToken))
        {
            return null;
        }

        return await BuildCartAsync(
            userId.Value,
            cancellationToken);
    }

    public async Task<CartMutationResult> AddAsync(
        ClaimsPrincipal principal,
        AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null ||
            !await UserExistsAsync(
                userId.Value,
                cancellationToken))
        {
            return new CartMutationResult(
                CartMutationStatus.Unauthorized);
        }
        var exactVariant = await dbContext.ProductVariants
            .Include(variant => variant.Product)
                .ThenInclude(product => product.Category)
            .Include(variant => variant.ProductSize)
            .Include(variant => variant.ProductColor)
            .SingleOrDefaultAsync(
                variant =>
                    variant.ProductSizeId ==
                        request.ProductSizeId &&
                    variant.ProductColorId ==
                        request.ProductColorId &&
                    variant.IsActive &&
                    variant.Product.IsActive &&
                    variant.Product.Category.IsActive,
                cancellationToken);

        if (exactVariant is null)
        {
            return new CartMutationResult(
                CartMutationStatus.NotFound);
        }

        var productSize = exactVariant.ProductSize;
        var productColor = exactVariant.ProductColor;

        var cartItem = await dbContext.CartItems
            .SingleOrDefaultAsync(
                item =>
                    item.UserId == userId.Value &&
                    item.ProductSizeId ==
                        request.ProductSizeId &&
                    item.ProductColorId ==
                        request.ProductColorId,
                cancellationToken);

        var newQuantity =
            (cartItem?.Quantity ?? 0) +
            request.Quantity;

        if (newQuantity > exactVariant.StockQuantity)
        {
            return new CartMutationResult(
                CartMutationStatus.OutOfStock);
        }

        if (cartItem is null)
        {
            dbContext.CartItems.Add(
                new CartItem
                {
                    UserId = userId.Value,
                    ProductSizeId =
                        request.ProductSizeId,
                    ProductColorId =
                        request.ProductColorId,
                    Quantity = request.Quantity,
                    CreatedAt =
                        DateTimeOffset.UtcNow
                });
        }
        else
{
    cartItem.Quantity = newQuantity;
    cartItem.UpdatedAt =
        DateTimeOffset.UtcNow;
}

await dbContext.SaveChangesAsync(
    cancellationToken);

await notificationService.CreateAsync(
    userId.Value,
    "Added to your bag",
    $"{productSize.Product.Name} in " +
    $"{productColor.Name}, size " +
    $"{productSize.Name}, was added to your bag.",
    "cart",
    "/cart",
    cancellationToken);

return new CartMutationResult(
            CartMutationStatus.Success,
            await BuildCartAsync(
                userId.Value,
                cancellationToken));
    }

    public async Task<CartMutationResult> UpdateAsync(
        ClaimsPrincipal principal,
        Guid cartItemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null ||
            !await UserExistsAsync(
                userId.Value,
                cancellationToken))
        {
            return new CartMutationResult(
                CartMutationStatus.Unauthorized);
        }

        var cartItem = await dbContext.CartItems
            .Include(item => item.ProductSize)
            .ThenInclude(size => size.Product)
            .ThenInclude(product => product.Category)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == cartItemId &&
                    item.UserId == userId.Value,
                cancellationToken);

        if (cartItem is null ||
            !cartItem.ProductSize.Product.IsActive ||
            !cartItem.ProductSize.Product.Category.IsActive)
        {
            return new CartMutationResult(
                CartMutationStatus.NotFound);
        }
        var exactVariant =
            cartItem.ProductVariant ??
            await dbContext.ProductVariants
                .SingleOrDefaultAsync(
                    variant =>
                        variant.ProductSizeId ==
                            cartItem.ProductSizeId &&
                        variant.ProductColorId ==
                            cartItem.ProductColorId &&
                        variant.IsActive,
                    cancellationToken);

        if (exactVariant is null)
        {
            return new CartMutationResult(
                CartMutationStatus.NotFound);
        }

        if (request.Quantity >
            exactVariant.StockQuantity)
        {
            return new CartMutationResult(
                CartMutationStatus.OutOfStock);
        }

        cartItem.ProductVariantId =
            exactVariant.Id;

        cartItem.Quantity = request.Quantity;
        cartItem.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new CartMutationResult(
            CartMutationStatus.Success,
            await BuildCartAsync(
                userId.Value,
                cancellationToken));
    }

    public async Task<CartMutationResult> RemoveAsync(
        ClaimsPrincipal principal,
        Guid cartItemId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null ||
            !await UserExistsAsync(
                userId.Value,
                cancellationToken))
        {
            return new CartMutationResult(
                CartMutationStatus.Unauthorized);
        }

        var cartItem = await dbContext.CartItems
            .SingleOrDefaultAsync(
                item =>
                    item.Id == cartItemId &&
                    item.UserId == userId.Value,
                cancellationToken);

        if (cartItem is null)
        {
            return new CartMutationResult(
                CartMutationStatus.NotFound);
        }

        dbContext.CartItems.Remove(cartItem);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new CartMutationResult(
            CartMutationStatus.Success,
            await BuildCartAsync(
                userId.Value,
                cancellationToken));
    }

    public async Task<CartMutationResult> ClearAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null ||
            !await UserExistsAsync(
                userId.Value,
                cancellationToken))
        {
            return new CartMutationResult(
                CartMutationStatus.Unauthorized);
        }

        var items = await dbContext.CartItems
            .Where(item =>
                item.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        dbContext.CartItems.RemoveRange(items);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new CartMutationResult(
            CartMutationStatus.Success,
            new CartResponse([], 0, 0m));
    }

    private async Task<CartResponse> BuildCartAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.CartItems
            .AsNoTracking()
            .Where(item =>
                item.UserId == userId &&
                item.ProductSize.Product.IsActive &&
                item.ProductSize.Product.Category.IsActive)
            .OrderByDescending(item =>
                item.CreatedAt)
            .Select(item =>
                new CartItemResponse(
                    item.Id,
                    item.ProductSize.Product.Id,
                    item.ProductSize.Id,
                    item.ProductColorId,
                    item.ProductVariantId,
                    item.ProductSize.Product.Name,
                    item.ProductSize.Product.Slug,
                    item.ProductSize.Product.Category.Name,
                    item.ProductSize.Product.Category.Audience,
                    item.ProductColor != null &&
                    item.ProductColor.ImageUrl != null
                        ? item.ProductColor.ImageUrl
                        : item.ProductSize.Product.ImageUrl,
                    item.ProductSize.Name,
                    item.ProductColor != null
                        ? item.ProductColor.Name
                        : null,
                    item.ProductColor != null
                        ? item.ProductColor.HexCode
                        : null,
                    item.ProductVariant != null
                        ? item.ProductVariant.Sku
                        : null,
                    item.ProductSize.Product.Promotions.Any(link =>
                        link.Promotion.IsActive &&
                        link.Promotion.StartsAt <=
                            DateTimeOffset.UtcNow &&
                        link.Promotion.EndsAt >=
                            DateTimeOffset.UtcNow)
                        ? item.ProductSize.Product.Price *
                            (1m -
                                item.ProductSize.Product.Promotions
                                    .Where(link =>
                                        link.Promotion.IsActive &&
                                        link.Promotion.StartsAt <=
                                            DateTimeOffset.UtcNow &&
                                        link.Promotion.EndsAt >=
                                            DateTimeOffset.UtcNow)
                                    .Max(link =>
                                        link.Promotion
                                            .DiscountPercentage) /
                                100m)
                        : item.ProductSize.Product.Price,
                    item.Quantity,
                    item.ProductVariant != null
                        ? item.ProductVariant.StockQuantity
                        : item.ProductSize.StockQuantity,
                    (item.ProductSize.Product.Promotions.Any(link =>
                        link.Promotion.IsActive &&
                        link.Promotion.StartsAt <=
                            DateTimeOffset.UtcNow &&
                        link.Promotion.EndsAt >=
                            DateTimeOffset.UtcNow)
                        ? item.ProductSize.Product.Price *
                            (1m -
                                item.ProductSize.Product.Promotions
                                    .Where(link =>
                                        link.Promotion.IsActive &&
                                        link.Promotion.StartsAt <=
                                            DateTimeOffset.UtcNow &&
                                        link.Promotion.EndsAt >=
                                            DateTimeOffset.UtcNow)
                                    .Max(link =>
                                        link.Promotion
                                            .DiscountPercentage) /
                                100m)
                        : item.ProductSize.Product.Price) *
                            item.Quantity))
            .ToListAsync(cancellationToken);

        return new CartResponse(
            items,
            items.Sum(item => item.Quantity),
            items.Sum(item => item.LineTotal));
    }

    private Task<bool> UserExistsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Id == userId,
            cancellationToken);
    }

    private static Guid? GetUserId(
        ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId)
            ? userId
            : null;
    }
}


