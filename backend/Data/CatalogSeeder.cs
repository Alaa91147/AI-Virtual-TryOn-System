using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Data;

public static class CatalogSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var categories = new List<Category>
        {
            CreateCategory(
                "Tops",
                "tops",
                CatalogAudiences.Women,
                1),

            CreateCategory(
                "Dresses",
                "dresses",
                CatalogAudiences.Women,
                2),

            CreateCategory(
                "Bottoms",
                "bottoms",
                CatalogAudiences.Women,
                3),

            CreateCategory(
                "Outerwear",
                "outerwear",
                CatalogAudiences.Women,
                4),

            CreateCategory(
                "Shoes",
                "shoes",
                CatalogAudiences.Women,
                5),

            CreateCategory(
                "Accessories",
                "accessories",
                CatalogAudiences.Women,
                6),

            CreateCategory(
                "Shirts",
                "shirts",
                CatalogAudiences.Men,
                1),

            CreateCategory(
                "T-Shirts & Polos",
                "polos",
                CatalogAudiences.Men,
                2),

            CreateCategory(
                "Trousers",
                "trousers",
                CatalogAudiences.Men,
                3),

            CreateCategory(
                "Outerwear",
                "outerwear",
                CatalogAudiences.Men,
                4),

            CreateCategory(
                "Shoes",
                "shoes",
                CatalogAudiences.Men,
                5),

            CreateCategory(
                "Accessories",
                "accessories",
                CatalogAudiences.Men,
                6),
        };

        var existingCategories =
            await dbContext.Categories
                .AsNoTracking()
                .Select(category => new
                {
                    category.Audience,
                    category.Slug
                })
                .ToListAsync(cancellationToken);

        var existingKeys = existingCategories
            .Select(category =>
                CreateKey(
                    category.Audience,
                    category.Slug))
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var missingCategories = categories
            .Where(category =>
                !existingKeys.Contains(
                    CreateKey(
                        category.Audience,
                        category.Slug)))
            .ToList();

        if (missingCategories.Count == 0)
        {
            return;
        }

        dbContext.Categories.AddRange(
            missingCategories);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static Category CreateCategory(
        string name,
        string slug,
        string audience,
        int displayOrder)
    {
        return new Category
        {
            Name = name,
            Slug = slug,
            Audience = audience,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string CreateKey(
        string audience,
        string slug)
    {
        return $"{audience}:{slug}";
    }
}