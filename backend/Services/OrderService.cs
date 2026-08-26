using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Orders;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public enum CheckoutStatus { Success, Unauthorized, EmptyCart, OutOfStock }
public sealed record CheckoutResult(CheckoutStatus Status, OrderResponse? Order = null);

public class OrderService(AppDbContext dbContext, NotificationService notificationService)
{
    public async Task<CheckoutResult> CheckoutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null) return new(CheckoutStatus.Unauthorized);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(
            () => CheckoutTransactionAsync(userId.Value, cancellationToken));

        if (result.Status == CheckoutStatus.Success && result.Order is not null)
        {
            await notificationService.CreateAsync(userId.Value, "Order received",
                $"Your order {result.Order.OrderNumber} was placed successfully.",
                "order", "/profile", cancellationToken);
        }

        return result;
    }

    private async Task<CheckoutResult> CheckoutTransactionAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var cartItems = await dbContext.CartItems
            .Include(item => item.ProductColor)
            .Include(item => item.ProductSize).ThenInclude(size => size.Product)
                .ThenInclude(product => product.Promotions).ThenInclude(link => link.Promotion)
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);
        if (cartItems.Count == 0) return new(CheckoutStatus.EmptyCart);
        if (cartItems.Any(item => item.Quantity > item.ProductSize.StockQuantity))
            return new(CheckoutStatus.OutOfStock);

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            UserId = userId,
            OrderNumber = $"VTO-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Status = OrderStatuses.Pending,
            CreatedAt = now
        };

        foreach (var cart in cartItems)
        {
            var product = cart.ProductSize.Product;
            var discount = product.Promotions
                .Where(link => link.Promotion.IsActive && link.Promotion.StartsAt <= now && link.Promotion.EndsAt >= now)
                .Select(link => link.Promotion.DiscountPercentage)
                .DefaultIfEmpty(0m).Max();
            var unitPrice = decimal.Round(product.Price * (1m - discount / 100m), 2);
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id, ProductSizeId = cart.ProductSizeId,
                ProductColorId = cart.ProductColorId, ProductName = product.Name,
                ImageUrl = product.ImageUrl, SizeName = cart.ProductSize.Name,
                ColorName = cart.ProductColor?.Name, OriginalUnitPrice = product.Price,
                UnitPrice = unitPrice, Quantity = cart.Quantity,
                LineTotal = unitPrice * cart.Quantity
            });
            cart.ProductSize.StockQuantity -= cart.Quantity;
        }

        order.Subtotal = order.Items.Sum(item => item.LineTotal);
        order.Total = order.Subtotal + order.DeliveryFee;
        dbContext.Orders.Add(order);
        dbContext.CartItems.RemoveRange(cartItems);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(CheckoutStatus.Success, await GetOrderAsync(order.Id, cancellationToken));
    }

    public async Task<IReadOnlyList<OrderResponse>> GetCustomerOrdersAsync(
        ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null) return [];
        var orders = await GetQuery().Where(order => order.UserId == userId.Value)
            .OrderByDescending(order => order.CreatedAt).ToListAsync(cancellationToken);
        return orders.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var orders = await GetQuery().OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
        return orders.Select(ToResponse).ToList();
    }

    public async Task<OrderResponse?> UpdateStatusAsync(
        Guid id, string status, string? trackingNumber, CancellationToken cancellationToken)
    {
        var normalized = OrderStatuses.All.FirstOrDefault(item =>
            string.Equals(item, status, StringComparison.OrdinalIgnoreCase));
        if (normalized is null) return null;

        var order = await dbContext.Orders.Include(item => item.Items)
            .ThenInclude(item => item.ProductSize)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null || order.Status == OrderStatuses.Cancelled) return null;

        if (normalized == OrderStatuses.Cancelled)
            foreach (var item in order.Items) item.ProductSize.StockQuantity += item.Quantity;

        order.Status = normalized;
        order.TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await notificationService.CreateAsync(order.UserId, $"Order {normalized.ToLowerInvariant()}",
            $"Your order {order.OrderNumber} is now {normalized.ToLowerInvariant()}.",
            "order", "/profile", cancellationToken);
        return await GetOrderAsync(id, cancellationToken);
    }

    private IQueryable<Order> GetQuery() => dbContext.Orders.AsNoTracking()
        .Include(order => order.User).ThenInclude(user => user.Profile)
        .Include(order => order.Items).AsSplitQuery();
    private async Task<OrderResponse?> GetOrderAsync(Guid id, CancellationToken token)
    {
        var order = await GetQuery().SingleOrDefaultAsync(order => order.Id == id, token);
        return order is null ? null : ToResponse(order);
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id, order.OrderNumber, order.UserId,
        order.User.Profile != null ? order.User.Profile.FullName : order.User.Email,
        order.User.Email, order.Status, order.Subtotal, order.DeliveryFee,
        order.Total, order.TrackingNumber, order.CreatedAt,
        order.Items.Select(item => new OrderItemResponse(
            item.Id, item.ProductId, item.ProductName, item.ImageUrl,
            item.SizeName, item.ColorName, item.OriginalUnitPrice,
            item.UnitPrice, item.Quantity, item.LineTotal)).ToList());

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
