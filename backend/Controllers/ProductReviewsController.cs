using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.DTOs.Reviews;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products/{productId:guid}/reviews")]
public class ProductReviewsController(
    ProductReviewService productReviewService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(ProductReviewsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReviewsResponse>>
        Get(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var reviews =
            await productReviewService.GetAsync(
                User,
                productId,
                cancellationToken);

        if (reviews is null)
        {
            return NotFound(
                ApiError.Create(
                    "Product not found.",
                    "The selected product does not exist."));
        }

        return Ok(reviews);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ProductReviewsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductReviewsResponse>>
        Create(
            Guid productId,
            CreateProductReviewRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await productReviewService.CreateAsync(
                User,
                productId,
                request,
                cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{reviewId:guid}")]
    [ProducesResponseType(
        typeof(ProductReviewsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReviewsResponse>>
        Update(
            Guid productId,
            Guid reviewId,
            UpdateProductReviewRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await productReviewService.UpdateAsync(
                User,
                productId,
                reviewId,
                request,
                cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{reviewId:guid}")]
    [ProducesResponseType(
        typeof(ProductReviewsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReviewsResponse>>
        Delete(
            Guid productId,
            Guid reviewId,
            CancellationToken cancellationToken)
    {
        var result =
            await productReviewService.DeleteAsync(
                User,
                productId,
                reviewId,
                cancellationToken);

        return ToActionResult(result);
    }

    private ActionResult<ProductReviewsResponse>
        ToActionResult(
            ReviewMutationResult result)
    {
        return result.Status switch
        {
            ReviewMutationStatus.Success =>
                Ok(result.Reviews),

            ReviewMutationStatus.Unauthorized =>
                Unauthorized(
                    ApiError.Create(
                        "You need to sign in again.",
                        "You need to sign in again.")),

            ReviewMutationStatus.NotFound =>
                NotFound(
                    ApiError.Create(
                        "Review not found.",
                        "The selected product or review does not exist.")),

            ReviewMutationStatus.AlreadyExists =>
                Conflict(
                    ApiError.Create(
                        "You already reviewed this product.",
                        "Edit your existing review instead.")),

            _ =>
                StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiError.Create(
                        "Could not update the review.",
                        "Please try again."))
        };
    }
}