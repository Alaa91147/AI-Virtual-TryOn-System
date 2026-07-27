using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Reviews;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public enum ReviewMutationStatus
{
    Success,
    Unauthorized,
    NotFound,
    AlreadyExists
}

public sealed record ReviewMutationResult(
    ReviewMutationStatus Status,
    ProductReviewsResponse? Reviews = null);

public class ProductReviewService(
    AppDbContext dbContext,
    NotificationService notificationService)
{
    public async Task<ProductReviewsResponse?>
        GetAsync(
            ClaimsPrincipal principal,
            Guid productId,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
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
            return null;
        }

        return await BuildResponseAsync(
            productId,
            userId.Value,
            cancellationToken);
    }

    public async Task<ReviewMutationResult>
        CreateAsync(
            ClaimsPrincipal principal,
            Guid productId,
            CreateProductReviewRequest request,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null ||
            !await UserExistsAsync(
                userId.Value,
                cancellationToken))
        {
            return new ReviewMutationResult(
                ReviewMutationStatus.Unauthorized);
        }

        var productExists =
            await dbContext.Products.AnyAsync(
                product =>
                    product.Id == productId &&
                    product.IsActive,
                cancellationToken);

        if (!productExists)
        {
            return new ReviewMutationResult(
                ReviewMutationStatus.NotFound);
        }

        var reviewExists =
            await dbContext.ProductReviews.AnyAsync(
                review =>
                    review.UserId == userId.Value &&
                    review.ProductId == productId,
                cancellationToken);

        if (reviewExists)
        {
            return new ReviewMutationResult(
                ReviewMutationStatus.AlreadyExists);
        }

        dbContext.ProductReviews.Add(
            new ProductReview
            {
                UserId = userId.Value,
                ProductId = productId,
                Rating = request.Rating,
                Comment = request.Comment.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await RecalculateProductRatingAsync(
    productId,
    cancellationToken);

await notificationService.CreateAsync(
    userId.Value,
    "Review published",
    $"Your {request.Rating}-star review was published successfully.",
    "review",
    $"/shop/products/{productId}",
    cancellationToken);

return new ReviewMutationResult(
            ReviewMutationStatus.Success,
            await BuildResponseAsync(
                productId,
                userId.Value,
                cancellationToken));
    }

    public async Task<ReviewMutationResult>
        UpdateAsync(
            ClaimsPrincipal principal,
            Guid productId,
            Guid reviewId,
            UpdateProductReviewRequest request,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null ||
            !await UserExistsAsync(
                userId.Value,
                cancellationToken))
        {
            return new ReviewMutationResult(
                ReviewMutationStatus.Unauthorized);
        }

        var review =
            await dbContext.ProductReviews
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == reviewId &&
                        item.ProductId == productId &&
                        item.UserId == userId.Value,
                    cancellationToken);

        if (review is null)
        {
            return new ReviewMutationResult(
                ReviewMutationStatus.NotFound);
        }

        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();
        review.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await RecalculateProductRatingAsync(
    productId,
    cancellationToken);

await notificationService.CreateAsync(
    userId.Value,
    "Review updated",
    $"Your review was updated to {request.Rating} stars.",
    "review",
    $"/shop/products/{productId}",
    cancellationToken);

return new ReviewMutationResult(
            ReviewMutationStatus.Success,
            await BuildResponseAsync(
                productId,
                userId.Value,
                cancellationToken));
    }

    public async Task<ReviewMutationResult>
        DeleteAsync(
            ClaimsPrincipal principal,
            Guid productId,
            Guid reviewId,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null ||
            !await UserExistsAsync(
                userId.Value,
                cancellationToken))
        {
            return new ReviewMutationResult(
                ReviewMutationStatus.Unauthorized);
        }

        var review =
            await dbContext.ProductReviews
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == reviewId &&
                        item.ProductId == productId &&
                        item.UserId == userId.Value,
                    cancellationToken);

        if (review is null)
        {
            return new ReviewMutationResult(
                ReviewMutationStatus.NotFound);
        }

        dbContext.ProductReviews.Remove(review);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await RecalculateProductRatingAsync(
            productId,
            cancellationToken);

        return new ReviewMutationResult(
            ReviewMutationStatus.Success,
            await BuildResponseAsync(
                productId,
                userId.Value,
                cancellationToken));
    }

    private async Task<ProductReviewsResponse>
        BuildResponseAsync(
            Guid productId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        var reviews =
            await dbContext.ProductReviews
                .AsNoTracking()
                .Where(review =>
                    review.ProductId == productId)
                .OrderByDescending(review =>
                    review.UpdatedAt ??
                    review.CreatedAt)
                .Select(review =>
                    new ProductReviewResponse(
                        review.Id,
                        review.UserId,
review.User.Profile != null
    ? review.User.Profile.FullName
    : review.User.FullName,
                            review.Rating,
                        review.Comment,
                        review.CreatedAt,
                        review.UpdatedAt,
                        review.UserId == userId))
                .ToListAsync(cancellationToken);

       var averageRating =
    reviews.Count == 0
        ? 0m
        : Math.Round(
            reviews.Sum(review =>
                review.Rating) /
            (decimal)reviews.Count,
            2);

        return new ProductReviewsResponse(
            productId,
            averageRating,
            reviews.Count,
            reviews.FirstOrDefault(review =>
                review.IsOwnReview),
            reviews);
    }

    private async Task
        RecalculateProductRatingAsync(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var product =
            await dbContext.Products
                .SingleAsync(
                    item => item.Id == productId,
                    cancellationToken);

        var statistics =
            await dbContext.ProductReviews
                .Where(review =>
                    review.ProductId == productId)
                .GroupBy(review =>
                    review.ProductId)
                .Select(group => new
                {
                    Count = group.Count(),
                    Average = group.Average(
                        review =>
                            (decimal)review.Rating)
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        product.ReviewCount =
            statistics?.Count ?? 0;

        product.Rating =
            statistics is null
                ? 0m
                : Math.Round(
                    statistics.Average,
                    2);

        product.UpdatedAt =
            DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);
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