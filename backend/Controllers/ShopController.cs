using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.DTOs.Shop;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ShopController(
    ShopService shopService)
    : ControllerBase
{
    [HttpGet("home")]
    [ProducesResponseType(
        typeof(ShopHomeResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShopHomeResponse>>
        GetHome(
            CancellationToken cancellationToken)
    {
        var response =
            await shopService.GetHomeAsync(
                User,
                cancellationToken);

        if (response is null)
        {
            return Unauthorized(
                ApiError.Create(
                    "You need to sign in again.",
                    "You need to sign in again."));
        }

        return Ok(response);
    }
}