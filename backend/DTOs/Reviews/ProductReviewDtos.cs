using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Reviews;

public sealed class CreateProductReviewRequest
{
    [Range(
        1,
        5,
        ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Comment is required.")]
    [StringLength(
        2000,
        MinimumLength = 3,
        ErrorMessage =
            "Comment must be between 3 and 2000 characters.")]
    public string Comment { get; set; } = string.Empty;
}

public sealed class UpdateProductReviewRequest
{
    [Range(
        1,
        5,
        ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Comment is required.")]
    [StringLength(
        2000,
        MinimumLength = 3,
        ErrorMessage =
            "Comment must be between 3 and 2000 characters.")]
    public string Comment { get; set; } = string.Empty;
}

public sealed record ProductReviewResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsOwnReview);

public sealed record ProductReviewsResponse(
    Guid ProductId,
    decimal AverageRating,
    int ReviewCount,
    ProductReviewResponse? CurrentUserReview,
    IReadOnlyList<ProductReviewResponse> Reviews);