using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/promotions")]
public class AdminPromotionsController(PromotionService promotionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromotionResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await promotionService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PromotionResponse>> Create(
        CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var result = await promotionService.CreateAsync(request, cancellationToken);
        return result is null
            ? BadRequest(ApiError.Create("Promotion could not be created.", "One or more selected products do not exist."))
            : Created($"/api/admin/promotions/{result.Id}", result);
    }

    [HttpPost("{id:guid}/send-email")]
    public async Task<ActionResult<PromotionResponse>> Send(Guid id, CancellationToken cancellationToken)
    {
        var result = await promotionService.SendCampaignAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<PromotionResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await promotionService.DeactivateAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
