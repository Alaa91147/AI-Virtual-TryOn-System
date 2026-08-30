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
public class AdminPromotionsController(
    PromotionService promotionService, AdminAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromotionResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await promotionService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PromotionResponse>> Create(
        CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var result = await promotionService.CreateAsync(request, cancellationToken);
        if (result is null) return BadRequest(ApiError.Create("Promotion could not be created.", "One or more selected products do not exist."));
        await Audit("Create", result.Id, result.Name, cancellationToken);
        return Created($"/api/admin/promotions/{result.Id}", result);
    }

    [HttpPost("{id:guid}/send-email")]
    public async Task<ActionResult<PromotionResponse>> Send(Guid id, CancellationToken cancellationToken)
    {
        var result = await promotionService.SendCampaignAsync(id, cancellationToken);
        return result is null
            ? NotFound(ApiError.Create("Promotion not found."))
            : Ok(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<PromotionResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await promotionService.DeactivateAsync(id, cancellationToken);
        if (result is not null) await Audit("Deactivate", id, result.Name, cancellationToken);
        return result is null
            ? NotFound(ApiError.Create("Promotion not found."))
            : Ok(result);
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<PromotionResponse>> Reactivate(Guid id, CancellationToken token)
    {
        var result = await promotionService.ReactivateAsync(id, token);
        if (result is null) return BadRequest(ApiError.Create("Promotion cannot be reactivated.", "It may not exist or its end date has passed."));
        await Audit("Reactivate", id, result.Name, token);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PromotionResponse>> Update(Guid id, UpdatePromotionRequest request, CancellationToken token)
    {
        var result = await promotionService.UpdateAsync(id, request, token);
        if (result is null) return BadRequest(ApiError.Create("Promotion could not be updated."));
        await Audit("Update", id, result.Name, token);
        return Ok(result);
    }

    private Task Audit(string action, Guid id, string details, CancellationToken token) =>
        auditService.RecordAsync(User, action, "Promotion", id, details,
            HttpContext.Connection.RemoteIpAddress?.ToString(), token);
}
