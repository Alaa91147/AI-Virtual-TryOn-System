using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.DTOs.Notifications;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificationsController(
    NotificationService notificationService)
    : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<NotificationListResponse>>
        Get(
            [FromQuery] int limit = 20,
            CancellationToken cancellationToken =
                default)
    {
        var response =
            await notificationService.GetAsync(
                User,
                limit,
                cancellationToken);

        if (response is null)
        {
            return Unauthorized(
                ApiError.Create(
                    "You need to sign in again.",
                    "You need to sign in again."));
        }

        return Ok(response);
    }

    [HttpPut("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result =
            await notificationService.MarkReadAsync(
                User,
                notificationId,
                cancellationToken);

        if (result is null)
        {
            return Unauthorized(
                ApiError.Create(
                    "You need to sign in again.",
                    "You need to sign in again."));
        }

        if (result == false)
        {
            return NotFound(
                ApiError.Create(
                    "Notification not found.",
                    "The notification does not exist."));
        }

        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(
        CancellationToken cancellationToken)
    {
        var result =
            await notificationService
                .MarkAllReadAsync(
                    User,
                    cancellationToken);

        if (result is null)
        {
            return Unauthorized(
                ApiError.Create(
                    "You need to sign in again.",
                    "You need to sign in again."));
        }

        return NoContent();
    }
}