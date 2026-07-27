using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Notifications;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public class NotificationService(
    AppDbContext dbContext)
{
    public async Task<NotificationListResponse?>
        GetAsync(
            ClaimsPrincipal principal,
            int limit,
            CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return null;
        }

        var userExists =
            await dbContext.Users.AnyAsync(
                user => user.Id == userId.Value,
                cancellationToken);

        if (!userExists)
        {
            return null;
        }

        var hasNotifications =
            await dbContext.Notifications.AnyAsync(
                notification =>
                    notification.UserId ==
                        userId.Value,
                cancellationToken);

        if (!hasNotifications)
        {
            dbContext.Notifications.Add(
                new UserNotification
                {
                    UserId = userId.Value,
                    Title = "Welcome to your style space",
                    Message =
                        "Your personalized shop, saved items, reviews, and bag are ready.",
                    Type = "welcome",
                    Link = "/shop",
                    CreatedAt =
                        DateTimeOffset.UtcNow
                });

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return await BuildResponseAsync(
            userId.Value,
            limit,
            cancellationToken);
    }

    public async Task<bool?> MarkReadAsync(
        ClaimsPrincipal principal,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return null;
        }

        var notification =
            await dbContext.Notifications
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == notificationId &&
                        item.UserId == userId.Value,
                    cancellationToken);

        if (notification is null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt =
                DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return true;
    }
public async Task CreateAsync(
    Guid userId,
    string title,
    string message,
    string type,
    string? link,
    CancellationToken cancellationToken)
{
    var userExists =
        await dbContext.Users.AnyAsync(
            user => user.Id == userId,
            cancellationToken);

    if (!userExists)
    {
        return;
    }

    dbContext.Notifications.Add(
        new UserNotification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type.Trim().ToLowerInvariant(),
            Link = string.IsNullOrWhiteSpace(link)
                ? null
                : link.Trim(),
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

    await dbContext.SaveChangesAsync(
        cancellationToken);
}

    public async Task<bool?> MarkAllReadAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        if (userId is null)
        {
            return null;
        }

        var notifications =
            await dbContext.Notifications
                .Where(notification =>
                    notification.UserId ==
                        userId.Value &&
                    !notification.IsRead)
                .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private async Task<NotificationListResponse>
        BuildResponseAsync(
            Guid userId,
            int limit,
            CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);

        var items =
            await dbContext.Notifications
                .AsNoTracking()
                .Where(notification =>
                    notification.UserId == userId)
                .OrderByDescending(notification =>
                    notification.CreatedAt)
                .Take(safeLimit)
                .Select(notification =>
                    new NotificationResponse(
                        notification.Id,
                        notification.Title,
                        notification.Message,
                        notification.Type,
                        notification.Link,
                        notification.IsRead,
                        notification.CreatedAt,
                        notification.ReadAt))
                .ToListAsync(cancellationToken);

        var unreadCount =
            await dbContext.Notifications
                .CountAsync(
                    notification =>
                        notification.UserId == userId &&
                        !notification.IsRead,
                    cancellationToken);

        return new NotificationListResponse(
            items,
            unreadCount);
    }

    private static Guid? GetUserId(
        ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId)
            ? userId
            : null;
    }
}