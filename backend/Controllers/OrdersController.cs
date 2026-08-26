using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.DTOs.Orders;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController, Authorize, Route("api/orders")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<OrderResponse>> Checkout(CancellationToken token)
    {
        var result = await orderService.CheckoutAsync(User, token);
        return result.Status switch
        {
            CheckoutStatus.Success => Created($"/api/orders/{result.Order!.Id}", result.Order),
            CheckoutStatus.EmptyCart => BadRequest(ApiError.Create("Your bag is empty.", "Add products before placing an order.")),
            CheckoutStatus.OutOfStock => Conflict(ApiError.Create("Stock changed.", "One or more items are no longer available in the requested quantity.")),
            _ => Unauthorized()
        };
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> Mine(CancellationToken token) =>
        Ok(await orderService.GetCustomerOrdersAsync(User, token));
}
