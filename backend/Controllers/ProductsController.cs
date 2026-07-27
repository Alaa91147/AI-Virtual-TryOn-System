using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.DTOs.Shop;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController(
    ProductService productService)
    : ControllerBase
{
    [HttpGet("{productId:guid}")]
    [ProducesResponseType(
        typeof(ShopProductResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiError),
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<ShopProductResponse>>
        GetProduct(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var product =
            await productService
                .GetProductAsync(
                    User,
                    productId,
                    cancellationToken);

        if (product is null)
        {
            return NotFound(
                ApiError.Create(
                    "Product not found.",
                    "The selected product does not exist."));
        }

        return Ok(product);
    }

    [HttpGet("recently-viewed")]
    [ProducesResponseType(
        typeof(
            IReadOnlyList<
                ShopProductResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<
            IReadOnlyList<
                ShopProductResponse>>>
        GetRecentlyViewed(
            [FromQuery] int limit = 12,
            CancellationToken cancellationToken =
                default)
    {
        var products =
            await productService
                .GetRecentlyViewedAsync(
                    User,
                    limit,
                    cancellationToken);

        return Ok(products);
    }
}