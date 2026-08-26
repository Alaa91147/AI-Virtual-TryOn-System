using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public enum DeleteProductStatus { Deleted, NotFound, HasOrderHistory }

public class AdminProductService(
    AppDbContext dbContext,
    IWebHostEnvironment environment)
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

        return new AdminCatalogResponse(
            categories,
            productEntities.Select(ToResponse).ToList());
    }

    public async Task<AdminProductResponse?> CreateAsync(
        SaveAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsRequestValidAsync(request, null, cancellationToken))
        {
            return null;
        }

        var originalImageUrl =
            CleanUrl(request.OriginalImageUrl) ?? request.ImageUrl.Trim();

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Slug = NormalizeSlug(request.Slug),
            Description = request.Description.Trim(),
            Price = request.Price,
            OriginalImageUrl = originalImageUrl,
            ImageUrl = originalImageUrl,
            AiMaskImageUrl = CleanUrl(request.AiMaskImageUrl),
            Badge = CleanBadge(request.Badge),
            IsNew = string.Equals(
                request.Badge, "New", StringComparison.OrdinalIgnoreCase),
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
            .SingleOrDefaultAsync(
                item => item.Id == productId,
                cancellationToken);

        if (product is null ||
            !await IsRequestValidAsync(request, productId, cancellationToken))
        {
            return null;
        }

        var obsoleteUploadUrls = product.Colors
            .Select(color => color.ImageUrl)
            .Append(product.AiMaskImageUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var originalImageUrl =
            CleanUrl(request.OriginalImageUrl)
            ?? CleanUrl(product.OriginalImageUrl)
            ?? request.ImageUrl.Trim();

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Slug = NormalizeSlug(request.Slug);
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.OriginalImageUrl = originalImageUrl;
        product.ImageUrl = originalImageUrl;
        product.AiMaskImageUrl = CleanUrl(request.AiMaskImageUrl);
        product.Badge = CleanBadge(request.Badge);
        product.IsNew = string.Equals(
            request.Badge, "New", StringComparison.OrdinalIgnoreCase);
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        MergeColors(product, request);
        MergeSizes(product, request);

        await dbContext.SaveChangesAsync(cancellationToken);
        await DeleteObsoleteUploadsAsync(
            obsoleteUploadUrls,
            cancellationToken);
        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<DeleteProductStatus> DeleteAsync(
        Guid productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null) return DeleteProductStatus.NotFound;

        if (await dbContext.OrderItems.AnyAsync(
                item => item.ProductId == productId, cancellationToken))
            return DeleteProductStatus.HasOrderHistory;

        var cartItems = await dbContext.CartItems
            .Where(item => item.ProductSize.ProductId == productId ||
                           (item.ProductColor != null && item.ProductColor.ProductId == productId))
            .ToListAsync(cancellationToken);
        dbContext.CartItems.RemoveRange(cartItems);
        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DeleteProductStatus.Deleted;
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
            .Include(product => product.Promotions)
            .ThenInclude(link => link.Promotion)
            .AsSplitQuery();

    private static AdminProductResponse ToResponse(Product product)
    {
        var promotion = product.Promotions
            .Where(link => link.Promotion.IsActive &&
                link.Promotion.StartsAt <= DateTimeOffset.UtcNow &&
                link.Promotion.EndsAt >= DateTimeOffset.UtcNow)
            .OrderByDescending(link => link.Promotion.DiscountPercentage)
            .Select(link => link.Promotion)
            .FirstOrDefault();

        return new(
            product.Id, product.CategoryId, product.Category.Name,
            product.Category.Audience, product.Name, product.Slug,
            product.Description,
            product.Price,
            promotion is null ? null : decimal.Round(
                product.Price * (1m - promotion.DiscountPercentage / 100m), 2),
            promotion?.DiscountPercentage,
            product.ImageUrl,
            string.IsNullOrWhiteSpace(product.OriginalImageUrl)
                ? product.ImageUrl
                : product.OriginalImageUrl,
            product.AiMaskImageUrl,
            product.Badge,
            product.IsNew,
            product.IsActive,
            product.Colors.OrderBy(color => color.Name)
                .Select(color => new AdminProductColorResponse(
                    color.Id,
                    color.Name,
                    color.HexCode,
                    color.ImageUrl))
                .ToList(),
            product.Sizes
                .OrderBy(size => size.Name)
                .Select(size => new AdminProductSizeResponse(
                    size.Id, size.Name, size.StockQuantity)).ToList());
    }

    private static List<ProductColor> CleanColors(
        SaveAdminProductRequest request) =>
        request.Colors
            .Where(color => !string.IsNullOrWhiteSpace(color.Name))
            .GroupBy(
                color => color.Name.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(color => new ProductColor
            {
                Name = color.Name.Trim(),
                HexCode = color.HexCode.Trim(),
                ImageUrl = CleanUrl(color.ImageUrl)
            })
            .ToList();

    private static List<ProductSize> CleanSizes(
        SaveAdminProductRequest request) =>
        request.Sizes
            .Where(size => !string.IsNullOrWhiteSpace(size.Name))
            .GroupBy(
                size => size.Name.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(size => new ProductSize
            {
                Name = size.Name.Trim(),
                StockQuantity = Math.Max(0, size.StockQuantity)
            })
            .ToList();

    private void MergeColors(
        Product product,
        SaveAdminProductRequest request)
    {
        foreach (var requested in CleanColors(request))
        {
            var existing = product.Colors.FirstOrDefault(color =>
                string.Equals(
                    color.Name,
                    requested.Name,
                    StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                requested.ProductId = product.Id;
                product.Colors.Add(requested);
                dbContext.Entry(requested).State = EntityState.Added;
            }
            else
            {
                existing.Name = requested.Name;
                existing.HexCode = requested.HexCode;
                existing.ImageUrl = requested.ImageUrl;
            }
        }
    }

    private void MergeSizes(
        Product product,
        SaveAdminProductRequest request)
    {
        foreach (var requested in CleanSizes(request))
        {
            var existing = product.Sizes.FirstOrDefault(size =>
                string.Equals(
                    size.Name,
                    requested.Name,
                    StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                requested.ProductId = product.Id;
                product.Sizes.Add(requested);
                dbContext.Entry(requested).State = EntityState.Added;
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

    private static string? CleanUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task DeleteObsoleteUploadsAsync(
        IEnumerable<string> previousUrls,
        CancellationToken cancellationToken)
    {
        foreach (var previousUrl in previousUrls.Distinct(
                     StringComparer.OrdinalIgnoreCase))
        {
            var stillReferenced =
                await dbContext.Products.AsNoTracking().AnyAsync(
                    product =>
                        product.ImageUrl == previousUrl ||
                        product.OriginalImageUrl == previousUrl ||
                        product.AiMaskImageUrl == previousUrl,
                    cancellationToken) ||
                await dbContext.ProductColors.AsNoTracking().AnyAsync(
                    color => color.ImageUrl == previousUrl,
                    cancellationToken);

            if (stillReferenced || !TryGetManagedUploadPath(
                    previousUrl,
                    out var uploadPath))
            {
                continue;
            }

            if (File.Exists(uploadPath))
            {
                File.Delete(uploadPath);
            }
        }
    }

    private bool TryGetManagedUploadPath(
        string imageUrl,
        out string uploadPath)
    {
        uploadPath = string.Empty;

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        const string uploadPrefix = "/uploads/products/";
        if (!uri.AbsolutePath.StartsWith(
                uploadPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fileName = Path.GetFileName(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var uploadDirectory = Path.GetFullPath(Path.Combine(
            environment.ContentRootPath,
            "wwwroot",
            "uploads",
            "products"));
        var candidatePath = Path.GetFullPath(Path.Combine(
            uploadDirectory,
            fileName));

        if (!candidatePath.StartsWith(
                uploadDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        uploadPath = candidatePath;
        return true;
    }
}
