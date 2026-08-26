using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController, Authorize(Roles = UserRoles.Admin), Route("api/admin/customers")]
public class AdminCustomersController(AdminCustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminCustomerResponse>>> GetAll(CancellationToken token) =>
        Ok(await customerService.GetAllAsync(token));

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<AdminCustomerResponse>> UpdateRole(
        Guid id, UpdateCustomerRoleRequest request, CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId)) return Unauthorized();
        var result = await customerService.UpdateRoleAsync(id, adminId, request.Role, token);
        return result.Status switch
        {
            CustomerUpdateStatus.Success => Ok(result.Customer),
            CustomerUpdateStatus.NotFound => NotFound(ApiError.Create("Customer not found.")),
            CustomerUpdateStatus.CannotChangeSelf => BadRequest(ApiError.Create("You cannot change your own admin role.")),
            _ => BadRequest(ApiError.Create("Role must be Customer or Admin."))
        };
    }

    [HttpPost("{id:guid}/notifications")]
    public async Task<IActionResult> SendNotification(
        Guid id, SendCustomerNotificationRequest request, CancellationToken token)
    {
        var sent = await customerService.SendNotificationAsync(id, request.Title.Trim(), request.Message.Trim(), token);
        return sent ? Ok(new { message = "Notification sent." }) : NotFound(ApiError.Create("Customer not found."));
    }
}
