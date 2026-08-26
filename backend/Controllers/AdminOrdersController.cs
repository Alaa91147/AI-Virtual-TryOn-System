using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.DTOs.Orders;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController, Authorize(Roles = UserRoles.Admin), Route("api/admin/orders")]
public class AdminOrdersController(OrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken token) =>
        Ok(await orderService.GetAllAsync(token));

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        Guid id, UpdateOrderStatusRequest request, CancellationToken token)
    {
        var result = await orderService.UpdateStatusAsync(id, request.Status, request.TrackingNumber, token);
        return result is null ? BadRequest() : Ok(result);
    }
}
