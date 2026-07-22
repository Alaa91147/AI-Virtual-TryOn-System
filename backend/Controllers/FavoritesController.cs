using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FavoritesController(
    FavoriteService favoriteService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<Guid>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status401Unauthorized)]
    public async Task<
        ActionResult<IReadOnlyList<Guid>>>
        GetFavorites(
            CancellationToken cancellationToken)
    {
        var productIds =
            await favoriteService
                .GetFavoriteProductIdsAsync(
                    User,
                    cancellationToken);

        if (productIds is null)
        {
            return Unauthorized(
                ApiError.Create(
                    "You need to sign in again.",
                    "You need to sign in again."));
        }

        return Ok(productIds);
    }

    [HttpPost("{productId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
        AddFavorite(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var result =
            await favoriteService.AddAsync(
                User,
                productId,
                cancellationToken);

        if (result is null)
        {
            return Unauthorized(
                ApiError.Create(
                    "You need to sign in again.",
                    "You need to sign in again."));
        }

        if (result == false)
        {
            return NotFound(
                ApiError.Create(
                    "Product not found.",
                    "The selected product does not exist."));
        }

        return NoContent();
    }

    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult>
        RemoveFavorite(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var result =
            await favoriteService.RemoveAsync(
                User,
                productId,
                cancellationToken);

        if (result is null)
        {
            return Unauthorized(
                ApiError.Create(
                    "You need to sign in again.",
                    "You need to sign in again."));
        }

        return NoContent();
    }
}