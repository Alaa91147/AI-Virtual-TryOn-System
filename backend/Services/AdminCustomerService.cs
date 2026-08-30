using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;

namespace VirtualTryOn.Api.Services;

public enum CustomerUpdateStatus
{
    Success,
    NotFound,
    InvalidRole,
    CannotChangeSelf
}

public sealed record CustomerUpdateResult(
    CustomerUpdateStatus Status,
    AdminCustomerResponse? Customer = null);

public enum CustomerAdministrationStatus
{
    Success,
    NotFound,
    CannotChangeSelf,
    CannotManageAdmin
}

public sealed record CustomerAdministrationResult(
    CustomerAdministrationStatus Status,
    AdminCustomerResponse? Customer = null);

public class AdminCustomerService(
    AppDbContext dbContext,
    NotificationService notificationService)
{
    public async Task<IReadOnlyList<AdminCustomerResponse>>
        GetAllAsync(CancellationToken token)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Profile)
            .Include(user => user.DeliveryAddress)
            .Include(user => user.Orders)
            .AsSplitQuery()
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync(token);

        return users.Select(ToResponse).ToList();
    }

    public async Task<CustomerUpdateResult> UpdateRoleAsync(
        Guid id,
        Guid currentAdminId,
        string requestedRole,
        CancellationToken token)
    {
        var role = new[]
            {
                UserRoles.Customer,
                UserRoles.Admin
            }
            .FirstOrDefault(value =>
                string.Equals(
                    value,
                    requestedRole,
                    StringComparison.OrdinalIgnoreCase));

        if (role is null)
            return new(CustomerUpdateStatus.InvalidRole);

        if (id == currentAdminId)
            return new(CustomerUpdateStatus.CannotChangeSelf);

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                item => item.Id == id,
                token);

        if (user is null)
            return new(CustomerUpdateStatus.NotFound);

        user.Role = role;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(token);

        return new(
            CustomerUpdateStatus.Success,
            await GetByIdAsync(id, token));
    }

    public async Task<bool> SendNotificationAsync(
        Guid id,
        string title,
        string message,
        CancellationToken token)
    {
        if (!await dbContext.Users.AnyAsync(
                user => user.Id == id,
                token))
        {
            return false;
        }

        await notificationService.CreateAsync(
            id,
            title,
            message,
            "admin",
            "/shop",
            token);

        return true;
    }

    public async Task<CustomerAdministrationResult>
        UpdateAdministrationAsync(
            Guid id,
            Guid currentAdminId,
            bool isSuspended,
            string? suspensionReason,
            DateTimeOffset? suspendedUntil,
            string? adminNotes,
            bool revokeSessions,
            CancellationToken token)
    {
        if (id == currentAdminId)
        {
            return new(
                CustomerAdministrationStatus
                    .CannotChangeSelf);
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                item => item.Id == id,
                token);

        if (user is null)
        {
            return new(
                CustomerAdministrationStatus.NotFound);
        }

        if (string.Equals(
                user.Role,
                UserRoles.Admin,
                StringComparison.OrdinalIgnoreCase))
        {
            return new(
                CustomerAdministrationStatus
                    .CannotManageAdmin);
        }

        user.IsSuspended = isSuspended;

        user.SuspensionReason = isSuspended
            ? Clean(suspensionReason)
            : null;

        user.SuspendedUntil = isSuspended
            ? suspendedUntil
            : null;

        user.AdminNotes = Clean(adminNotes);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (isSuspended || revokeSessions)
        {
            var refreshTokens =
                await dbContext.RefreshTokens
                    .Where(item =>
                        item.UserId == id &&
                        item.RevokedAt == null)
                    .ToListAsync(token);

            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.RevokedAt =
                    DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(token);

        return new(
            CustomerAdministrationStatus.Success,
            await GetByIdAsync(id, token));
    }

    private async Task<AdminCustomerResponse?> GetByIdAsync(
        Guid id,
        CancellationToken token)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(item => item.Profile)
            .Include(item => item.DeliveryAddress)
            .Include(item => item.Orders)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                item => item.Id == id,
                token);

        return user is null
            ? null
            : ToResponse(user);
    }

    private static AdminCustomerResponse ToResponse(
        Models.User user)
    {
        var completedOrders = user.Orders
            .Where(order =>
                order.Status != OrderStatuses.Cancelled)
            .ToList();

        var address =
            user.DeliveryAddress is null
                ? null
                : string.Join(
                    ", ",
                    new[]
                    {
                        user.DeliveryAddress.Street,
                        user.DeliveryAddress.Building,
                        user.DeliveryAddress.City,
                        user.DeliveryAddress.Country
                    }.Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

        return new AdminCustomerResponse(
            user.Id,
            user.Profile?.FullName ?? user.FullName,
            user.Email,
            user.Role,
            user.IsEmailVerified,
            !string.IsNullOrWhiteSpace(
                user.GoogleSubject),
            user.Profile?.Gender ?? user.Gender,
            user.ShoppingPreference,
            user.Profile?.PhoneNumber ??
                user.DeliveryAddress?.PhoneNumber,
            string.IsNullOrWhiteSpace(address)
                ? null
                : address,
            user.CreatedAt,
            user.Orders.Count,
            completedOrders.Sum(order => order.Total),
            user.Orders
                .OrderByDescending(order => order.CreatedAt)
                .Select(order =>
                    (DateTimeOffset?)order.CreatedAt)
                .FirstOrDefault(),
            user.IsSuspended,
            user.SuspensionReason,
            user.SuspendedUntil,
            user.AdminNotes);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
