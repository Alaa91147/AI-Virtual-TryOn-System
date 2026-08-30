using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController, Authorize(Roles = UserRoles.Admin), Route("api/admin/categories")]
public sealed class AdminCategoriesController(
    AdminCategoryService categoryService, AdminAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryDetailResponse>>> GetAll(CancellationToken token) =>
        Ok(await categoryService.GetAllAsync(token));

    [HttpPost]
    public async Task<ActionResult<AdminCategoryDetailResponse>> Create(SaveAdminCategoryRequest request, CancellationToken token)
    {
        var result = await categoryService.CreateAsync(request, token);
        if (result.Status != CategorySaveStatus.Success)
            return Conflict(ApiError.Create("A category with this slug and audience already exists."));
        await Audit("Create", result.Category!.Id, result.Category.Name, token);
        return Created($"/api/admin/categories/{result.Category.Id}", result.Category);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCategoryDetailResponse>> Update(Guid id, SaveAdminCategoryRequest request, CancellationToken token)
    {
        var result = await categoryService.UpdateAsync(id, request, token);
        if (result.Status == CategorySaveStatus.Success)
        {
            await Audit("Update", id, result.Category!.Name, token);
            return Ok(result.Category);
        }
        return result.Status switch
        {
            CategorySaveStatus.NotFound => NotFound(ApiError.Create("Category not found.")),
            _ => Conflict(ApiError.Create("A category with this slug and audience already exists."))
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        var result = await categoryService.DeleteAsync(id, token);
        if (result == CategorySaveStatus.Success)
        {
            await Audit("Delete", id, null, token);
            return NoContent();
        }
        return result switch
        {
            CategorySaveStatus.NotFound => NotFound(ApiError.Create("Category not found.")),
            _ => Conflict(ApiError.Create("This category contains products and cannot be deleted.", "Deactivate it instead."))
        };
    }

    private Task Audit(string action, Guid id, string? details, CancellationToken token) =>
        auditService.RecordAsync(User, action, "Category", id, details,
            HttpContext.Connection.RemoteIpAddress?.ToString(), token);
}
