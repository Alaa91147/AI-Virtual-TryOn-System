using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Models;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/variant-inventory")]
public sealed class AdminVariantInventoryController(
    AppDbContext db,
    AdminAuditService audit) : ControllerBase
{
    [HttpGet("variants")]
    public async Task<
        ActionResult<IReadOnlyList<AdminInventoryVariantResponse>>>
        GetVariants(CancellationToken token)
    {
        var variants = await db.ProductVariants
            .AsNoTracking()
            .OrderBy(variant => variant.Product.Name)
            .ThenBy(variant => variant.ProductColor.Name)
            .ThenBy(variant => variant.ProductSize.Name)
            .Select(variant =>
                new AdminInventoryVariantResponse(
                    variant.Id,
                    variant.ProductId,
                    variant.Product.Name,
                    variant.Product.ImageUrl,
                    variant.Product.Category.Name,
                    variant.ProductColorId,
                    variant.ProductColor.Name,
                    variant.ProductColor.HexCode,
                    variant.ProductColor.ImageUrl,
                    variant.ProductSizeId,
                    variant.ProductSize.Name,
                    variant.Sku,
                    variant.StockQuantity,
                    variant.LowStockThreshold,
                    variant.StockQuantity <=
                        variant.LowStockThreshold,
                    variant.IsActive))
            .ToListAsync(token);

        return Ok(variants);
    }

    [HttpPost("synchronize")]
    public async Task<ActionResult<SynchronizeVariantsResponse>>
        Synchronize(CancellationToken token)
    {
        // Load catalog data without tracking to prevent EF from
        // interpreting generated variants as modified records.
        var products = await db.Products
            .AsNoTracking()
            .Include(product => product.Colors)
            .Include(product => product.Sizes)
            .AsSplitQuery()
            .ToListAsync(token);

        var existingVariants = await db.ProductVariants
            .AsNoTracking()
            .Select(variant => new
            {
                variant.ProductId,
                variant.ProductColorId,
                variant.ProductSizeId
            })
            .ToListAsync(token);

        var existingKeys = existingVariants
            .Select(variant =>
                $"{variant.ProductId:N}|" +
                $"{variant.ProductColorId:N}|" +
                $"{variant.ProductSizeId:N}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newVariants = new List<ProductVariant>();

        foreach (var product in products)
        {
            var colors = product.Colors
                .OrderBy(color => color.Name)
                .ToList();

            var sizes = product.Sizes
                .OrderBy(size => size.Name)
                .ToList();

            if (colors.Count == 0 || sizes.Count == 0)
            {
                continue;
            }

            var firstColorId = colors[0].Id;

            foreach (var color in colors)
            {
                foreach (var size in sizes)
                {
                    var key =
                        $"{product.Id:N}|" +
                        $"{color.Id:N}|" +
                        $"{size.Id:N}";

                    if (existingKeys.Contains(key))
                    {
                        continue;
                    }

                    var initialQuantity = size.StockQuantity;

                    newVariants.Add(new ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        ProductColorId = color.Id,
                        ProductSizeId = size.Id,
                        Sku = CreateSku(
                            product,
                            color,
                            size),
                        StockQuantity = initialQuantity,
                        LowStockThreshold = 5,
                        IsActive = product.IsActive,
                        CreatedAt = DateTimeOffset.UtcNow
                    });

                    existingKeys.Add(key);
                }
            }
        }

        var actuallyCreatedCount = newVariants.Count;

        if (newVariants.Count > 0)
        {
            try
            {
                await db.ProductVariants.AddRangeAsync(
                    newVariants,
                    token);

                await db.SaveChangesAsync(token);
            }
            catch (DbUpdateException exception)
                when (
                    exception.InnerException is SqlException sqlException &&
                    (sqlException.Number == 2601 ||
                     sqlException.Number == 2627)
                )
            {
                // Another synchronization request created the same
                // variants first. Clear the failed tracked inserts and
                // return the current database state without a 500.
                db.ChangeTracker.Clear();
                actuallyCreatedCount = 0;
            }
        }

        var totalCount = await db.ProductVariants
            .AsNoTracking()
            .CountAsync(token);

        await audit.RecordAsync(
            User,
            "SynchronizeVariants",
            "ProductVariant",
            null,
            $"Created {actuallyCreatedCount} product variants.",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            token);

        return Ok(new SynchronizeVariantsResponse(
            actuallyCreatedCount,
            existingVariants.Count,
            totalCount));
    }
    [HttpGet("history")]
    public async Task<
        ActionResult<
            IReadOnlyList<VariantInventoryAdjustmentResponse>>>
        GetHistory(
            [FromQuery] int limit = 200,
            CancellationToken token = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);

        var history = await db.VariantInventoryAdjustments
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Take(safeLimit)
            .Select(item =>
                new VariantInventoryAdjustmentResponse(
                    item.Id,
                    item.ProductVariantId,
                    item.ProductVariant.ProductId,
                    item.ProductVariant.Product.Name,
                    item.ProductVariant.Product.ImageUrl,
                    item.ProductVariant.Product.Category.Name,
                    item.ProductVariant.ProductColor.Name,
                    item.ProductVariant.ProductColor.HexCode,
                    item.ProductVariant.ProductSize.Name,
                    item.ProductVariant.Sku,
                    item.Operation,
                    item.EnteredQuantity,
                    item.PreviousQuantity,
                    item.NewQuantity,
                    item.NewQuantity - item.PreviousQuantity,
                    item.Reason,
                    item.AdminUserId,
                    item.AdminUser.FullName,
                    item.AdminUser.Email,
                    item.CreatedAt))
            .ToListAsync(token);

        return Ok(history);
    }

    [HttpPost("variants/{variantId:guid}/adjust")]
    public async Task<
        ActionResult<VariantInventoryAdjustmentResponse>>
        Adjust(
            Guid variantId,
            AdjustVariantInventoryRequest request,
            CancellationToken token)
    {
        var variant = await db.ProductVariants
            .Include(item => item.Product)
                .ThenInclude(product => product.Category)
            .Include(item => item.ProductColor)
            .Include(item => item.ProductSize)
            .SingleOrDefaultAsync(
                item => item.Id == variantId,
                token);

        if (variant is null)
        {
            return NotFound(new
            {
                message = "Product variant not found."
            });
        }

        if (!variant.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "Stock cannot be changed for an inactive variant."
            });
        }

        var operation = NormalizeOperation(request.Operation);
        var previousQuantity = variant.StockQuantity;

        var newQuantity = operation switch
        {
            "Add" => previousQuantity + request.Quantity,
            "Remove" => previousQuantity - request.Quantity,
            "Set" => request.Quantity,
            _ => previousQuantity
        };

        if (newQuantity < 0)
        {
            return BadRequest(new
            {
                message =
                    $"Cannot remove {request.Quantity}. " +
                    $"Only {previousQuantity} item(s) are available."
            });
        }

        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var adminId))
        {
            return Unauthorized();
        }

        var admin = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Id == adminId,
                token);

        if (admin is null)
        {
            return Unauthorized();
        }

        variant.StockQuantity = newQuantity;
        variant.UpdatedAt = DateTimeOffset.UtcNow;

        var adjustment =
            new VariantInventoryAdjustment
            {
                ProductVariantId = variant.Id,
                AdminUserId = adminId,
                Operation = operation,
                EnteredQuantity = request.Quantity,
                PreviousQuantity = previousQuantity,
                NewQuantity = newQuantity,
                Reason = request.Reason.Trim()
            };

        db.VariantInventoryAdjustments.Add(adjustment);

        await db.SaveChangesAsync(token);

        var readableDetails =
            $"{variant.Product.Name} / " +
            $"{variant.ProductColor.Name} / " +
            $"{variant.ProductSize.Name}; " +
            $"SKU={variant.Sku}; " +
            $"Stock {previousQuantity} to {newQuantity}; " +
            $"Reason={adjustment.Reason}";

        await audit.RecordAsync(
            User,
            "AdjustVariantInventory",
            "ProductVariant",
            variant.Id,
            readableDetails,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            token);

        return Ok(
            new VariantInventoryAdjustmentResponse(
                adjustment.Id,
                variant.Id,
                variant.ProductId,
                variant.Product.Name,
                variant.Product.ImageUrl,
                variant.Product.Category.Name,
                variant.ProductColor.Name,
                variant.ProductColor.HexCode,
                variant.ProductSize.Name,
                variant.Sku,
                adjustment.Operation,
                adjustment.EnteredQuantity,
                adjustment.PreviousQuantity,
                adjustment.NewQuantity,
                adjustment.Difference,
                adjustment.Reason,
                adminId,
                admin.FullName,
                admin.Email,
                adjustment.CreatedAt));
    }

    private static string NormalizeOperation(string operation)
    {
        if (string.Equals(
                operation,
                "Add",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Add";
        }

        if (string.Equals(
                operation,
                "Remove",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Remove";
        }

        if (string.Equals(
                operation,
                "Set",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Set";
        }

        return operation.Trim();
    }

    private static string CreateSku(
        Product product,
        ProductColor color,
        ProductSize size)
    {
        var productPart = CleanSkuPart(product.Slug);
        var colorPart = CleanSkuPart(color.Name);
        var sizePart = CleanSkuPart(size.Name);

        var productReference = product.Id
            .ToString("N")[..6]
            .ToUpperInvariant();

        var sku =
            $"{productPart}-{colorPart}-{sizePart}-" +
            productReference;

        return sku.Length <= 80
            ? sku
            : sku[..80];
    }

    private static string CleanSkuPart(string value)
    {
        var cleaned = Regex.Replace(
                value.Trim().ToUpperInvariant(),
                "[^A-Z0-9]+",
                "-")
            .Trim('-');

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "ITEM";
        }

        return cleaned.Length <= 20
            ? cleaned
            : cleaned[..20].TrimEnd('-');
    }
}




