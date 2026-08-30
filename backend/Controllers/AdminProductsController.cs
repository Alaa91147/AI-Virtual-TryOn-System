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
    IWebHostEnvironment environment,
    AdminAuditService auditService) : ControllerBase
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
        if (product is null) return Conflict(ApiError.Create(
                "Product could not be saved.",
                "Check the category and ensure the slug is unique in that category."));
        await Audit("Create", product.Id, product.Name, cancellationToken);
        return Created($"/api/admin/products/{product.Id}", product);
    }

    [HttpPut("{productId:guid}")]
    public async Task<ActionResult<AdminProductResponse>> Update(
        Guid productId,
        SaveAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await productService.UpdateAsync(
            productId, request, cancellationToken);
        if (product is null) return NotFound(ApiError.Create(
                "Product could not be updated.",
                "The product/category was not found or the slug is already used."));
        await Audit("Update", product.Id, product.Name, cancellationToken);
        return Ok(product);
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

        await using (var validationStream = image.OpenReadStream())
        {
            var header = new byte[12];
            var bytesRead = await validationStream.ReadAsync(
                header.AsMemory(0, header.Length), cancellationToken);

            if (!HasValidImageSignature(header, bytesRead, image.ContentType))
            {
                return BadRequest(ApiError.Create(
                    "Invalid product image.",
                    "The file contents do not match its JPG, PNG, or WEBP type."));
            }
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

    private static bool HasValidImageSignature(
        byte[] header, int length, string contentType)
    {
        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return length >= 8 && header[..8].SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        return length >= 12
            && header[..4].SequenceEqual("RIFF"u8.ToArray())
            && header[8..12].SequenceEqual("WEBP"u8.ToArray());
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Delete(
        Guid productId, CancellationToken cancellationToken)
    {
        var result = await productService.DeleteAsync(productId, cancellationToken);
        if (result == DeleteProductStatus.Deleted)
            await Audit("Delete", productId, null, cancellationToken);
        return result switch
        {
            DeleteProductStatus.Deleted => NoContent(),
            DeleteProductStatus.NotFound => NotFound(ApiError.Create("Product not found.")),
            _ => Conflict(ApiError.Create(
                "This product cannot be permanently deleted.",
                "It appears in customer order history. Archive it instead."))
        };
    }

    private Task Audit(string action, Guid id, string? details, CancellationToken token) =>
        auditService.RecordAsync(User, action, "Product", id, details,
            HttpContext.Connection.RemoteIpAddress?.ToString(), token);
}
