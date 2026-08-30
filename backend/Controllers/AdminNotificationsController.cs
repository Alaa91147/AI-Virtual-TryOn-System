using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.DTOs.Notifications;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/notifications")]
public sealed class AdminNotificationsController(
    NotificationService notificationService, AdminAuditService auditService)
    : ControllerBase
{
    [HttpPost("broadcast")]
    public async Task<ActionResult<BroadcastNotificationResponse>> Broadcast(
        BroadcastNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await notificationService.BroadcastAsync(request, cancellationToken);
        await auditService.RecordAsync(User, "Broadcast", "Notification", null,
            $"Audience={request.Audience}; Recipients={result.RecipientCount}",
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(result);
    }
}
