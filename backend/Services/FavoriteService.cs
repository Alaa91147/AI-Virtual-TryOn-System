using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public class FavoriteService(
    AppDbContext dbContext)
{
    public async Task<IReadOnlyList<Guid>?>
        GetFavoriteProductIdsAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return null;
        }

        var userExists =
            await dbContext.Users.AnyAsync(
                user => user.Id == userId.Value,
                cancellationToken);

        if (!userExists)
        {
            return null;
        }

        return await dbContext.Favorites
            .AsNoTracking()
            .Where(favorite =>
                favorite.UserId == userId.Value)
            .OrderByDescending(favorite =>
                favorite.CreatedAt)
            .Select(favorite =>
                favorite.ProductId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool?> AddAsync(
        ClaimsPrincipal principal,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return null;
        }

        var userExists =
            await dbContext.Users.AnyAsync(
                user => user.Id == userId.Value,
                cancellationToken);

        if (!userExists)
        {
            return null;
        }

        var productExists =
            await dbContext.Products.AnyAsync(
                product =>
                    product.Id == productId &&
                    product.IsActive,
                cancellationToken);

        if (!productExists)
        {
            return false;
        }

        var alreadyFavorite =
            await dbContext.Favorites.AnyAsync(
                favorite =>
                    favorite.UserId ==
                        userId.Value &&
                    favorite.ProductId ==
                        productId,
                cancellationToken);

        if (alreadyFavorite)
        {
            return true;
        }

        dbContext.Favorites.Add(
            new Favorite
            {
                UserId = userId.Value,
                ProductId = productId,
                CreatedAt =
                    DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<bool?>
        RemoveAsync(
            ClaimsPrincipal principal,
            Guid productId,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return null;
        }

        var userExists =
            await dbContext.Users.AnyAsync(
                user => user.Id == userId.Value,
                cancellationToken);

        if (!userExists)
        {
            return null;
        }

        var favorite =
            await dbContext.Favorites
                .SingleOrDefaultAsync(
                    item =>
                        item.UserId ==
                            userId.Value &&
                        item.ProductId ==
                            productId,
                    cancellationToken);

        if (favorite is not null)
        {
            dbContext.Favorites.Remove(
                favorite);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return true;
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