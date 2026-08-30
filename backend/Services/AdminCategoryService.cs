using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public enum CategorySaveStatus { Success, NotFound, Duplicate, HasProducts }
public sealed record CategorySaveResult(CategorySaveStatus Status, AdminCategoryDetailResponse? Category = null);

public sealed class AdminCategoryService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<AdminCategoryDetailResponse>> GetAllAsync(CancellationToken token) =>
        await dbContext.Categories.AsNoTracking()
            .OrderBy(item => item.Audience).ThenBy(item => item.DisplayOrder)
            .Select(item => new AdminCategoryDetailResponse(item.Id, item.Name, item.Slug,
                item.Audience, item.ImageUrl, item.DisplayOrder, item.IsActive, item.Products.Count))
            .ToListAsync(token);

    public async Task<CategorySaveResult> CreateAsync(SaveAdminCategoryRequest request, CancellationToken token)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var audience = request.Audience.Trim().ToLowerInvariant();
        if (await dbContext.Categories.AnyAsync(item => item.Slug == slug && item.Audience == audience, token))
            return new(CategorySaveStatus.Duplicate);

        var category = new Category();
        Apply(category, request, slug, audience);
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(token);
        return new(CategorySaveStatus.Success, await GetAsync(category.Id, token));
    }

    public async Task<CategorySaveResult> UpdateAsync(Guid id, SaveAdminCategoryRequest request, CancellationToken token)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == id, token);
        if (category is null) return new(CategorySaveStatus.NotFound);
        var slug = request.Slug.Trim().ToLowerInvariant();
        var audience = request.Audience.Trim().ToLowerInvariant();
        if (await dbContext.Categories.AnyAsync(item => item.Id != id && item.Slug == slug && item.Audience == audience, token))
            return new(CategorySaveStatus.Duplicate);
        Apply(category, request, slug, audience);
        await dbContext.SaveChangesAsync(token);
        return new(CategorySaveStatus.Success, await GetAsync(id, token));
    }

    public async Task<CategorySaveStatus> DeleteAsync(Guid id, CancellationToken token)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == id, token);
        if (category is null) return CategorySaveStatus.NotFound;
        if (await dbContext.Products.AnyAsync(item => item.CategoryId == id, token)) return CategorySaveStatus.HasProducts;
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(token);
        return CategorySaveStatus.Success;
    }

    private async Task<AdminCategoryDetailResponse?> GetAsync(Guid id, CancellationToken token) =>
        await dbContext.Categories.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new AdminCategoryDetailResponse(item.Id, item.Name, item.Slug,
                item.Audience, item.ImageUrl, item.DisplayOrder, item.IsActive, item.Products.Count))
            .SingleOrDefaultAsync(token);

    private static void Apply(Category category, SaveAdminCategoryRequest request, string slug, string audience)
    {
        category.Name = request.Name.Trim(); category.Slug = slug; category.Audience = audience;
        category.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        category.DisplayOrder = request.DisplayOrder; category.IsActive = request.IsActive;
    }
}
