using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Orders;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public enum CheckoutStatus { Success, Unauthorized, EmptyCart, OutOfStock }
public sealed record CheckoutResult(CheckoutStatus Status, OrderResponse? Order = null);
public enum OrderUpdateStatus
{
    Success, NotFound, InvalidStatus, InvalidTransition, TrackingRequired
}
public sealed record OrderUpdateResult(
    OrderUpdateStatus Status,
    OrderResponse? Order = null);

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
        if (cartItems.Count == 0)
            return new(CheckoutStatus.EmptyCart);

        var productIds = cartItems
            .Select(item => item.ProductSize.ProductId)
            .Distinct()
            .ToList();

        var variants = await dbContext.ProductVariants
            .Where(variant =>
                productIds.Contains(variant.ProductId) &&
                variant.IsActive)
            .ToListAsync(cancellationToken);

        var variantLookup = variants.ToDictionary(
            variant => (
                variant.ProductId,
                variant.ProductColorId,
                variant.ProductSizeId));

        foreach (var cartItem in cartItems)
        {
            if (!cartItem.ProductColorId.HasValue)
                return new(CheckoutStatus.OutOfStock);

            var key = (
                cartItem.ProductSize.ProductId,
                cartItem.ProductColorId.Value,
                cartItem.ProductSizeId);

            if (!variantLookup.TryGetValue(
                    key,
                    out var exactVariant) ||
                cartItem.Quantity >
                    exactVariant.StockQuantity)
            {
                return new(CheckoutStatus.OutOfStock);
            }

            cartItem.ProductVariantId =
                exactVariant.Id;
        }

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
            var exactVariant = variantLookup[(
                product.Id,
                cart.ProductColorId!.Value,
                cart.ProductSizeId)];

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductSizeId = cart.ProductSizeId,
                ProductColorId = cart.ProductColorId,
                ProductVariantId = exactVariant.Id,
                ProductName = product.Name,
                ImageUrl =
                    cart.ProductColor?.ImageUrl ??
                    product.ImageUrl,
                SizeName = cart.ProductSize.Name,
                ColorName = cart.ProductColor?.Name,
                Sku = exactVariant.Sku,
                OriginalUnitPrice = product.Price,
                UnitPrice = unitPrice,
                Quantity = cart.Quantity,
                LineTotal = unitPrice * cart.Quantity
            });

            exactVariant.StockQuantity -=
                cart.Quantity;

            exactVariant.UpdatedAt =
                DateTimeOffset.UtcNow;
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

    public async Task<OrderUpdateResult> UpdateStatusAsync(
        Guid id, string status, string? trackingNumber, string? shippingCarrier,
        string? fulfillmentNotes, CancellationToken cancellationToken)
    {
        var normalized = OrderStatuses.All.FirstOrDefault(item =>
            string.Equals(item, status, StringComparison.OrdinalIgnoreCase));
        if (normalized is null) return new(OrderUpdateStatus.InvalidStatus);
        var order = await dbContext.Orders
            .Include(item => item.Items)
                .ThenInclude(item => item.ProductSize)
            .Include(item => item.Items)
                .ThenInclude(item => item.ProductVariant)
            .SingleOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);
        if (order is null) return new(OrderUpdateStatus.NotFound);

        var cleanedTracking = string.IsNullOrWhiteSpace(trackingNumber)
            ? null
            : trackingNumber.Trim();

        if ((normalized == OrderStatuses.Shipped || normalized == OrderStatuses.Delivered)
            && string.IsNullOrWhiteSpace(cleanedTracking))
        {
            return new(OrderUpdateStatus.TrackingRequired);
        }

        if (!IsAllowedTransition(order.Status, normalized))
            return new(OrderUpdateStatus.InvalidTransition);
        if (normalized == OrderStatuses.Cancelled)
        {
            foreach (var item in order.Items)
            {
                if (item.ProductVariant is not null)
                {
                    item.ProductVariant.StockQuantity +=
                        item.Quantity;

                    item.ProductVariant.UpdatedAt =
                        DateTimeOffset.UtcNow;
                }
                else
                {
                    // Compatibility for older orders.
                    item.ProductSize.StockQuantity +=
                        item.Quantity;
                }
            }
        }

        order.Status = normalized;
        order.TrackingNumber = cleanedTracking;
        order.ShippingCarrier = string.IsNullOrWhiteSpace(shippingCarrier) ? null : shippingCarrier.Trim();
        order.FulfillmentNotes = string.IsNullOrWhiteSpace(fulfillmentNotes) ? null : fulfillmentNotes.Trim();
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await notificationService.CreateAsync(order.UserId, $"Order {normalized.ToLowerInvariant()}",
            $"Your order {order.OrderNumber} is now {normalized.ToLowerInvariant()}.",
            "order", "/profile", cancellationToken);
        return new(OrderUpdateStatus.Success, await GetOrderAsync(id, cancellationToken));
    }

    private static bool IsAllowedTransition(string current, string requested)
    {
        if (string.Equals(current, requested, StringComparison.OrdinalIgnoreCase))
            return true;

        return current switch
        {
            OrderStatuses.Pending => requested is OrderStatuses.Confirmed or OrderStatuses.Cancelled,
            OrderStatuses.Confirmed => requested is OrderStatuses.Processing or OrderStatuses.Cancelled,
            OrderStatuses.Processing => requested is OrderStatuses.Shipped or OrderStatuses.Cancelled,
            OrderStatuses.Shipped => requested == OrderStatuses.Delivered,
            _ => false
        };
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
        order.Total, order.TrackingNumber, order.ShippingCarrier, order.FulfillmentNotes, order.CreatedAt,
        order.Items.Select(item => new OrderItemResponse(
            item.Id,
            item.ProductId,
            item.ProductVariantId,
            item.ProductName,
            item.ImageUrl,
            item.SizeName,
            item.ColorName,
            item.Sku,
            item.OriginalUnitPrice,
            item.UnitPrice,
            item.Quantity,
            item.LineTotal)).ToList());

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}


