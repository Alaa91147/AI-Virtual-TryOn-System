using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/products")]
public class AdminProductsController(
    AdminProductService productService,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminCatalogResponse>> GetCatalog(
        CancellationToken cancellationToken) =>
        Ok(await productService.GetCatalogAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AdminProductResponse>> Create(
        SaveAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);
        return product is null
            ? Conflict(ApiError.Create(
                "Product could not be saved.",
                "Check the category and ensure the slug is unique in that category."))
            : Created($"/api/admin/products/{product.Id}", product);
    }

    [HttpPut("{productId:guid}")]
    public async Task<ActionResult<AdminProductResponse>> Update(
        Guid productId,
        SaveAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await productService.UpdateAsync(
            productId, request, cancellationToken);
        return product is null
            ? NotFound(ApiError.Create(
                "Product could not be updated.",
                "The product/category was not found or the slug is already used."))
            : Ok(product);
    }

    [HttpPost("image")]
    [RequestSizeLimit(5_242_880)]
    public async Task<ActionResult<object>> UploadImage(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        if (image.Length == 0 || image.Length > 5_242_880 ||
            !allowedTypes.Contains(image.ContentType))
        {
            return BadRequest(ApiError.Create(
                "Invalid product image.",
                "Choose a JPG, PNG, or WEBP image smaller than 5 MB."));
        }

        var extension = image.ContentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var directory = Path.Combine(
            environment.ContentRootPath, "wwwroot", "uploads", "products");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);

        await using var stream = System.IO.File.Create(path);
        await image.CopyToAsync(stream, cancellationToken);

        var imageUrl = $"{Request.Scheme}://{Request.Host}" +
                       $"/uploads/products/{fileName}";
        return Ok(new { imageUrl });
    }
}
