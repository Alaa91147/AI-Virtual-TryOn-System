using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;

namespace VirtualTryOn.Api.Controllers;

[ApiController, Authorize(Roles = UserRoles.Admin), Route("api/admin/dashboard")]
public sealed class AdminDashboardController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminDashboardResponse>> Get(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken token)
    {
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.AddDays(-29);
        if (start >= end || end - start > TimeSpan.FromDays(366))
            return BadRequest(new { message = "Choose a date range between 1 and 366 days." });

        var orders = await dbContext.Orders.AsNoTracking()
            .Where(item => item.CreatedAt >= start && item.CreatedAt <= end)
            .Select(item => new { item.CreatedAt, item.Total, item.Status }).ToListAsync(token);
        var validOrders = orders.Where(item => item.Status != OrderStatuses.Cancelled).ToList();
        var revenueTrend = validOrders.GroupBy(item => DateOnly.FromDateTime(item.CreatedAt.UtcDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new AdminRevenuePoint(group.Key, group.Sum(item => item.Total), group.Count())).ToList();
        var pipeline = OrderStatuses.All.Select(status => new AdminOrderStatusPoint(
            status, orders.Count(item => item.Status == status))).ToList();
        var customerCount = await dbContext.Users.CountAsync(item => item.Role == UserRoles.Customer, token);
        var newCustomers = await dbContext.Users.CountAsync(item => item.Role == UserRoles.Customer
            && item.CreatedAt >= start && item.CreatedAt <= end, token);
        var activeProducts = await dbContext.Products.CountAsync(item => item.IsActive, token);
        var stocks = await dbContext.Products.AsNoTracking().Select(item => new
            { item.Id, Stock = item.Sizes.Select(size => size.StockQuantity).DefaultIfEmpty(0).Sum() }).ToListAsync(token);
        var activePromotions = await dbContext.Promotions.CountAsync(item => item.IsActive
            && item.StartsAt <= end && item.EndsAt >= start, token);

        return Ok(new AdminDashboardResponse(start, end, validOrders.Sum(item => item.Total),
            orders.Count, orders.Count(item => item.Status == OrderStatuses.Pending), customerCount,
            newCustomers, activeProducts, stocks.Count(item => item.Stock < 6), activePromotions,
            revenueTrend, pipeline));
    }
}
