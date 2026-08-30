using VirtualTryOn.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/customers")]
public class AdminCustomersController(
    AdminCustomerService customerService,
    AdminAuditService auditService,
    AuthService authService,
    AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<IReadOnlyList<AdminCustomerResponse>>>
        GetAll(CancellationToken token)
    {
        return Ok(
            await customerService.GetAllAsync(token));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<AdminCustomerResponse>>
        UpdateRole(
            Guid id,
            UpdateCustomerRoleRequest request,
            CancellationToken token)
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        var result =
            await customerService.UpdateRoleAsync(
                id,
                adminId,
                request.Role,
                token);

        if (result.Status ==
            CustomerUpdateStatus.Success)
        {
            await auditService.RecordAsync(
                User,
                "UpdateRole",
                "User",
                id,
                $"Role changed to {request.Role}.",
                GetIp(),
                token);
        }

        return result.Status switch
        {
            CustomerUpdateStatus.Success =>
                Ok(result.Customer),

            CustomerUpdateStatus.NotFound =>
                NotFound(ApiError.Create(
                    "Customer not found.")),

            CustomerUpdateStatus.CannotChangeSelf =>
                BadRequest(ApiError.Create(
                    "You cannot change your own role.")),

            _ => BadRequest(ApiError.Create(
                "Role must be Customer or Admin."))
        };
    }

    [HttpPost("{id:guid}/notifications")]
    public async Task<IActionResult> SendNotification(
        Guid id,
        SendCustomerNotificationRequest request,
        CancellationToken token)
    {
        var sent =
            await customerService.SendNotificationAsync(
                id,
                request.Title.Trim(),
                request.Message.Trim(),
                token);

        if (!sent)
        {
            return NotFound(
                ApiError.Create(
                    "Customer not found."));
        }

        await auditService.RecordAsync(
            User,
            "SendNotification",
            "User",
            id,
            $"Notification sent: {request.Title.Trim()}",
            GetIp(),
            token);

        return Ok(new
        {
            message = "Notification sent."
        });
    }

    [HttpPut("{id:guid}/administration")]
    public async Task<ActionResult<AdminCustomerResponse>>
        UpdateAdministration(
            Guid id,
            UpdateCustomerAdministrationRequest request,
            CancellationToken token)
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        var result =
            await customerService
                .UpdateAdministrationAsync(
                    id,
                    adminId,
                    request.IsSuspended,
                    request.SuspensionReason,
                    request.SuspendedUntil,
                    request.AdminNotes,
                    request.RevokeSessions,
                    token);

        if (result.Status ==
            CustomerAdministrationStatus.Success)
        {
            var action = request.IsSuspended
                ? "SuspendCustomer"
                : "ReactivateCustomer";

            var description = request.IsSuspended
                ? $"Account suspended. Reason: " +
                  $"{request.SuspensionReason}"
                : "Account reactivated.";

            if (request.RevokeSessions)
            {
                description +=
                    " Active sessions revoked.";
            }

            await auditService.RecordAsync(
                User,
                action,
                "Customer",
                id,
                description,
                GetIp(),
                token);
        }

        return result.Status switch
        {
            CustomerAdministrationStatus.Success =>
                Ok(result.Customer),

            CustomerAdministrationStatus.NotFound =>
                NotFound(ApiError.Create(
                    "Customer not found.")),

            CustomerAdministrationStatus
                .CannotChangeSelf =>
                BadRequest(ApiError.Create(
                    "You cannot manage your own account here.")),

            CustomerAdministrationStatus
                .CannotManageAdmin =>
                BadRequest(ApiError.Create(
                    "Administrator accounts cannot be " +
                    "managed from Customer Controls.")),

            _ => BadRequest(
                ApiError.Create(
                    "Account update failed."))
        };
    }

    private bool TryGetAdminId(out Guid adminId)
    {
        return Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out adminId);
    }

    private string? GetIp()
    {
        return HttpContext.Connection
            .RemoteIpAddress
            ?.ToString();
    }

    [HttpPost("{id:guid}/resend-verification")]
    public async Task<IActionResult>
        ResendVerification(
            Guid id,
            CancellationToken token)
    {
        var customer = await dbContext.Users
            .AsNoTracking()
            .Where(account => account.Id == id)
            .Select(account => new
            {
                account.Id,
                account.Email,
                account.Role,
                account.GoogleSubject,
                account.IsEmailVerified
            })
            .SingleOrDefaultAsync(token);

        if (customer is null)
        {
            return NotFound(
                ApiError.Create(
                    "Customer not found."));
        }

        if (customer.Role == UserRoles.Admin)
        {
            return BadRequest(
                ApiError.Create(
                    "Administrator verification cannot be changed here."));
        }

        if (customer.IsEmailVerified)
        {
            return BadRequest(
                ApiError.Create(
                    "This account is already verified."));
        }

        if (!string.IsNullOrWhiteSpace(
                customer.GoogleSubject))
        {
            return BadRequest(
                ApiError.Create(
                    "Google accounts are verified by Google."));
        }

        var sent =
            await authService.SendVerificationEmailAsync(
                customer.Id,
                token);

        if (!sent)
        {
            return BadRequest(
                ApiError.Create(
                    "A verification email was not required."));
        }

        await auditService.RecordAsync(
            User,
            "ResendVerification",
            "User",
            customer.Id,
            $"Verification email resent to {customer.Email}.",
            GetIp(),
            token);

        return Ok(
            new ApiMessage(
                "Verification email sent."));
    }

    [HttpPut("{id:guid}/verification")]
    public async Task<IActionResult>
        VerifyManually(
            Guid id,
            ManualEmailVerificationRequest request,
            CancellationToken token)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        if (id == adminId)
        {
            return BadRequest(
                ApiError.Create(
                    "You cannot manually verify your own account."));
        }

        var reason = request.Reason.Trim();

        if (reason.Length < 5)
        {
            return BadRequest(
                ApiError.Create(
                    "A verification reason is required."));
        }

        var customer =
            await dbContext.Users
                .SingleOrDefaultAsync(
                    account => account.Id == id,
                    token);

        if (customer is null)
        {
            return NotFound(
                ApiError.Create(
                    "Customer not found."));
        }

        if (customer.Role == UserRoles.Admin)
        {
            return BadRequest(
                ApiError.Create(
                    "Administrator verification cannot be changed here."));
        }

        if (customer.IsEmailVerified)
        {
            return BadRequest(
                ApiError.Create(
                    "This account is already verified."));
        }

        customer.IsEmailVerified = true;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        var unusedTokens =
            await dbContext.EmailVerificationTokens
                .Where(item =>
                    item.UserId == id &&
                    item.UsedAt == null)
                .ToListAsync(token);

        foreach (var unusedToken in unusedTokens)
        {
            unusedToken.UsedAt =
                DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(token);

        await auditService.RecordAsync(
            User,
            "ManualEmailVerification",
            "User",
            customer.Id,
            $"Email {customer.Email} manually verified. Reason: {reason}",
            GetIp(),
            token);

        return Ok(
            new ApiMessage(
                "Customer email verified manually."));
    }}



