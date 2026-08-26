using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;

namespace VirtualTryOn.Api.Services;

public enum CustomerUpdateStatus { Success, NotFound, InvalidRole, CannotChangeSelf }
public sealed record CustomerUpdateResult(CustomerUpdateStatus Status, AdminCustomerResponse? Customer = null);

public class AdminCustomerService(AppDbContext dbContext, NotificationService notificationService)
{
    public async Task<IReadOnlyList<AdminCustomerResponse>> GetAllAsync(CancellationToken token)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Include(user => user.Profile)
            .Include(user => user.DeliveryAddress)
            .Include(user => user.Orders)
            .AsSplitQuery()
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync(token);

        return users.Select(ToResponse).ToList();
    }

    public async Task<CustomerUpdateResult> UpdateRoleAsync(
        Guid id, Guid currentAdminId, string requestedRole, CancellationToken token)
    {
        var role = new[] { UserRoles.Customer, UserRoles.Admin }
            .FirstOrDefault(value => string.Equals(value, requestedRole, StringComparison.OrdinalIgnoreCase));
        if (role is null) return new(CustomerUpdateStatus.InvalidRole);
        if (id == currentAdminId) return new(CustomerUpdateStatus.CannotChangeSelf);

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == id, token);
        if (user is null) return new(CustomerUpdateStatus.NotFound);

        user.Role = role;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(token);
        return new(CustomerUpdateStatus.Success, await GetByIdAsync(id, token));
    }

    public async Task<bool> SendNotificationAsync(
        Guid id, string title, string message, CancellationToken token)
    {
        if (!await dbContext.Users.AnyAsync(user => user.Id == id, token)) return false;
        await notificationService.CreateAsync(id, title, message, "admin", "/shop", token);
        return true;
    }

    private async Task<AdminCustomerResponse?> GetByIdAsync(Guid id, CancellationToken token)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Include(item => item.Profile).Include(item => item.DeliveryAddress).Include(item => item.Orders)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id, token);
        return user is null ? null : ToResponse(user);
    }

    private static AdminCustomerResponse ToResponse(Models.User user)
    {
        var completedOrders = user.Orders.Where(order => order.Status != OrderStatuses.Cancelled).ToList();
        var address = user.DeliveryAddress is null ? null : string.Join(", ", new[]
        {
            user.DeliveryAddress.Street, user.DeliveryAddress.Building,
            user.DeliveryAddress.City, user.DeliveryAddress.Country
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new AdminCustomerResponse(
            user.Id,
            user.Profile?.FullName ?? user.FullName,
            user.Email,
            user.Role,
            user.IsEmailVerified,
            !string.IsNullOrWhiteSpace(user.GoogleSubject),
            user.Profile?.Gender ?? user.Gender,
            user.ShoppingPreference,
            user.Profile?.PhoneNumber ?? user.DeliveryAddress?.PhoneNumber,
            string.IsNullOrWhiteSpace(address) ? null : address,
            user.CreatedAt,
            user.Orders.Count,
            completedOrders.Sum(order => order.Total),
            user.Orders.OrderByDescending(order => order.CreatedAt).Select(order => (DateTimeOffset?)order.CreatedAt).FirstOrDefault());
    }
}
