using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.DTOs.Orders;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController, Authorize(Roles = UserRoles.Admin), Route("api/admin/orders")]
public class AdminOrdersController(
    OrderService orderService, AdminAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken token) =>
        Ok(await orderService.GetAllAsync(token));

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        Guid id, UpdateOrderStatusRequest request, CancellationToken token)
    {
        var result = await orderService.UpdateStatusAsync(
    id,
    request.Status,
    request.TrackingNumber,
    request.ShippingCarrier,
    request.FulfillmentNotes,
    token);

        if (result.Status == OrderUpdateStatus.Success)
        {
            await auditService.RecordAsync(User, "UpdateStatus", "Order", id,
                $"Status={request.Status}; Tracking={request.TrackingNumber}",
                HttpContext.Connection.RemoteIpAddress?.ToString(), token);
            return Ok(result.Order);
        }

        return result.Status switch
        {
            OrderUpdateStatus.NotFound => NotFound(
                ApiError.Create("Order not found.")),
            OrderUpdateStatus.InvalidStatus => BadRequest(
                ApiError.Create("Invalid order status.")),
            OrderUpdateStatus.TrackingRequired => BadRequest(
                ApiError.Create(
                    "A tracking number is required before shipping this order.")),
            _ => Conflict(ApiError.Create(
                "This order status change is not allowed.",
                "Move orders through Confirmed, Processing, Shipped, and Delivered in order."))
        };
    }
}
