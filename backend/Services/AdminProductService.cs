using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public class AdminProductService(AppDbContext dbContext)
{
    public async Task<AdminCatalogResponse> GetCatalogAsync(
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Audience)
            .ThenBy(category => category.DisplayOrder)
            .Select(category => new AdminCategoryResponse(
                category.Id, category.Name, category.Slug, category.Audience))
            .ToListAsync(cancellationToken);

        var productEntities = await ProductQuery()
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync(cancellationToken);

        var products = productEntities.Select(ToResponse).ToList();

        return new AdminCatalogResponse(categories, products);
    }

    public async Task<AdminProductResponse?> CreateAsync(
        SaveAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsRequestValidAsync(request, null, cancellationToken))
        {
            return null;
        }

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Slug = NormalizeSlug(request.Slug),
            Description = request.Description.Trim(),
            Price = request.Price,
            ImageUrl = request.ImageUrl.Trim(),
            Badge = CleanBadge(request.Badge),
            IsNew = string.Equals(request.Badge, "New", StringComparison.OrdinalIgnoreCase),
            IsActive = request.IsActive,
            Colors = CleanColors(request),
            Sizes = CleanSizes(request)
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<AdminProductResponse?> UpdateAsync(
        Guid productId,
        SaveAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(item => item.Colors)
            .Include(item => item.Sizes)
            .SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);

        if (product is null ||
            !await IsRequestValidAsync(request, productId, cancellationToken))
        {
            return null;
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Slug = NormalizeSlug(request.Slug);
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl.Trim();
        product.Badge = CleanBadge(request.Badge);
        product.IsNew = string.Equals(request.Badge, "New", StringComparison.OrdinalIgnoreCase);
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        MergeColors(product, request);
        MergeSizes(product, request);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(product.Id, cancellationToken);
    }

    private async Task<bool> IsRequestValidAsync(
        SaveAdminProductRequest request,
        Guid? productId,
        CancellationToken cancellationToken)
    {
        var slug = NormalizeSlug(request.Slug);
        return await dbContext.Categories.AnyAsync(
                   category => category.Id == request.CategoryId,
                   cancellationToken) &&
               !await dbContext.Products.AnyAsync(
                   product => product.CategoryId == request.CategoryId &&
                              product.Slug == slug &&
                              product.Id != productId,
                   cancellationToken);
    }

    private async Task<AdminProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await ProductQuery()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return product is null ? null : ToResponse(product);
    }

    private IQueryable<Product> ProductQuery() =>
        dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Colors)
            .Include(product => product.Sizes)
            .AsSplitQuery();

    private static AdminProductResponse ToResponse(Product product) =>
        new(
            product.Id, product.CategoryId, product.Category.Name,
            product.Category.Audience, product.Name, product.Slug,
            product.Description, product.Price, product.ImageUrl,
            product.Badge, product.IsNew, product.IsActive,
            product.Colors.OrderBy(color => color.Name)
                .Select(color => new AdminProductColorResponse(
                    color.Id, color.Name, color.HexCode)).ToList(),
            product.Sizes.OrderBy(size => size.Name)
                .Select(size => new AdminProductSizeResponse(
                    size.Id, size.Name, size.StockQuantity)).ToList());

    private static List<ProductColor> CleanColors(SaveAdminProductRequest request) =>
        request.Colors
            .Where(color => !string.IsNullOrWhiteSpace(color.Name))
            .GroupBy(color => color.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(color => new ProductColor
            {
                Name = color.Name.Trim(),
                HexCode = color.HexCode.Trim()
            }).ToList();

    private static List<ProductSize> CleanSizes(SaveAdminProductRequest request) =>
        request.Sizes
            .Where(size => !string.IsNullOrWhiteSpace(size.Name))
            .GroupBy(size => size.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(size => new ProductSize
            {
                Name = size.Name.Trim(),
                StockQuantity = Math.Max(0, size.StockQuantity)
            }).ToList();

    private static void MergeColors(
        Product product,
        SaveAdminProductRequest request)
    {
        foreach (var requested in CleanColors(request))
        {
            var existing = product.Colors.FirstOrDefault(color =>
                string.Equals(color.Name, requested.Name,
                    StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                product.Colors.Add(requested);
            }
            else
            {
                existing.Name = requested.Name;
                existing.HexCode = requested.HexCode;
            }
        }
    }

    private static void MergeSizes(
        Product product,
        SaveAdminProductRequest request)
    {
        foreach (var requested in CleanSizes(request))
        {
            var existing = product.Sizes.FirstOrDefault(size =>
                string.Equals(size.Name, requested.Name,
                    StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                product.Sizes.Add(requested);
            }
            else
            {
                existing.Name = requested.Name;
                existing.StockQuantity = requested.StockQuantity;
            }
        }
    }

    private static string NormalizeSlug(string value) =>
        value.Trim().ToLowerInvariant();

    private static string? CleanBadge(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
