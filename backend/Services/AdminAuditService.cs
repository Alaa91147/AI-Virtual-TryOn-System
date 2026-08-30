using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public sealed class AdminAuditService(
    AppDbContext dbContext)
{
    public async Task RecordAsync(
        ClaimsPrincipal principal,
        string action,
        string entityType,
        object? entityId,
        string? details,
        string? ipAddress,
        CancellationToken token)
    {
        if (!Guid.TryParse(
                principal.FindFirstValue(
                    ClaimTypes.NameIdentifier),
                out var adminId))
        {
            return;
        }

        var log = new AdminAuditLog
        {
            AdminUserId = adminId,
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId?.ToString(),
            Details = string.IsNullOrWhiteSpace(details)
                ? null
                : details.Trim(),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress)
                ? null
                : ipAddress.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AdminAuditLogs.Add(log);

        await dbContext.SaveChangesAsync(token);
    }

    public async Task<
        IReadOnlyList<AdminAuditLogResponse>>
        GetRecentAsync(
            int limit,
            CancellationToken token)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);

        var logs = await (
            from log in dbContext.AdminAuditLogs
                .AsNoTracking()
            join administrator in dbContext.Users
                .AsNoTracking()
                on log.AdminUserId equals administrator.Id
            orderby log.CreatedAt descending
            select new AdminAuditLogResponse(
                log.Id,
                log.AdminUserId,
                string.IsNullOrWhiteSpace(
                    administrator.FullName)
                    ? administrator.Email
                    : administrator.FullName,
                administrator.Email,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Details,
                log.IpAddress,
                log.CreatedAt
            ))
            .Take(safeLimit)
            .ToListAsync(token);

        return logs;
    }
}
