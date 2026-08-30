using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Models;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController, Authorize(Roles = UserRoles.Admin), Route("api/admin/inventory")]
public sealed class AdminInventoryController(AppDbContext db, AdminAuditService audit) : ControllerBase
{
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<InventoryAdjustmentResponse>>> History(
        [FromQuery] Guid? productId, [FromQuery] int limit = 200, CancellationToken token = default)
    {
        var query = db.InventoryAdjustments.AsNoTracking();
        if (productId.HasValue) query = query.Where(item => item.ProductId == productId);
        return Ok(await query.OrderByDescending(item => item.CreatedAt).Take(Math.Clamp(limit, 1, 500))
            .Select(item => new InventoryAdjustmentResponse(item.Id, item.ProductId, item.Product.Name,
                item.ProductSizeId, item.ProductSize.Name, item.PreviousQuantity, item.NewQuantity,
                item.NewQuantity - item.PreviousQuantity, item.Reason, item.AdminUserId, item.CreatedAt))
            .ToListAsync(token));
    }

    [HttpPost("sizes/{sizeId:guid}/adjust")]
    public async Task<ActionResult<InventoryAdjustmentResponse>> Adjust(
        Guid sizeId, AdjustInventoryRequest request, CancellationToken token)
    {
        var size = await db.ProductSizes.Include(item => item.Product)
            .SingleOrDefaultAsync(item => item.Id == sizeId, token);
        if (size is null) return NotFound(new { message = "Product size not found." });
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adjustment = new InventoryAdjustment { ProductId = size.ProductId, ProductSizeId = size.Id,
            AdminUserId = adminId, PreviousQuantity = size.StockQuantity, NewQuantity = request.NewQuantity,
            Reason = request.Reason.Trim() };
        size.StockQuantity = request.NewQuantity; db.InventoryAdjustments.Add(adjustment);
        await audit.RecordAsync(User, "AdjustInventory", "ProductSize", size.Id,
            $"{adjustment.PreviousQuantity} -> {adjustment.NewQuantity}; {adjustment.Reason}",
            HttpContext.Connection.RemoteIpAddress?.ToString(), token);
        await db.SaveChangesAsync(token);
        return Ok(new InventoryAdjustmentResponse(adjustment.Id, size.ProductId, size.Product.Name, size.Id,
            size.Name, adjustment.PreviousQuantity, adjustment.NewQuantity, adjustment.Difference,
            adjustment.Reason, adminId, adjustment.CreatedAt));
    }
}
