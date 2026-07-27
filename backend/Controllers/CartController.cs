using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.DTOs.Cart;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CartController(
    CartService cartService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartResponse>> Get(
        CancellationToken cancellationToken)
    {
        var cart = await cartService.GetAsync(
            User,
            cancellationToken);

        if (cart is null)
        {
            return Unauthorized(
                ApiError.Create(
                    "You need to sign in again.",
                    "You need to sign in again."));
        }

        return Ok(cart);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartResponse>> Add(
        AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await cartService.AddAsync(
            User,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{cartItemId:guid}")]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartResponse>> Update(
        Guid cartItemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await cartService.UpdateAsync(
            User,
            cartItemId,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{cartItemId:guid}")]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>> Remove(
        Guid cartItemId,
        CancellationToken cancellationToken)
    {
        var result = await cartService.RemoveAsync(
            User,
            cartItemId,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(
        typeof(CartResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CartResponse>> Clear(
        CancellationToken cancellationToken)
    {
        var result = await cartService.ClearAsync(
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    private ActionResult<CartResponse> ToActionResult(
        CartMutationResult result)
    {
        return result.Status switch
        {
            CartMutationStatus.Success =>
                Ok(result.Cart),

            CartMutationStatus.Unauthorized =>
                Unauthorized(
                    ApiError.Create(
                        "You need to sign in again.",
                        "You need to sign in again.")),

            CartMutationStatus.NotFound =>
                NotFound(
                    ApiError.Create(
                        "Cart item not found.",
                        "The selected product or cart item does not exist.")),

            CartMutationStatus.OutOfStock =>
                Conflict(
                    ApiError.Create(
                        "Not enough stock.",
                        "The requested quantity is not available.")),

            _ =>
                StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiError.Create(
                        "Could not update the cart.",
                        "Please try again."))
        };
    }
}