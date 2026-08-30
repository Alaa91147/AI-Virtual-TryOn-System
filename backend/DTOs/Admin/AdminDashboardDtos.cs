namespace VirtualTryOn.Api.DTOs.Admin;

public sealed record AdminDashboardResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    decimal Revenue,
    int OrderCount,
    int PendingOrderCount,
    int CustomerCount,
    int NewCustomerCount,
    int ActiveProductCount,
    int LowStockProductCount,
    int ActivePromotionCount,
    IReadOnlyList<AdminRevenuePoint> RevenueTrend,
    IReadOnlyList<AdminOrderStatusPoint> OrderPipeline);

public sealed record AdminRevenuePoint(DateOnly Date, decimal Revenue, int Orders);
public sealed record AdminOrderStatusPoint(string Status, int Count);
