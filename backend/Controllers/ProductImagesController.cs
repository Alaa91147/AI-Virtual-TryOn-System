using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("uploads/products")]
public class ProductImagesController(IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpGet("{fileName}")]
    public IActionResult GetImage(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(safeName, fileName, StringComparison.Ordinal))
        {
            return BadRequest();
        }

        var path = Path.Combine(
            environment.ContentRootPath,
            "wwwroot", "uploads", "products", safeName);

        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var contentType = Path.GetExtension(safeName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(path, contentType);
    }
}
