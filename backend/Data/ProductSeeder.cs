using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Data;

public static class ProductSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var categories =
            await dbContext.Categories
                .Where(category =>
                    category.IsActive)
                .ToListAsync(cancellationToken);

        Category GetCategory(
            string audience,
            string slug)
        {
            return categories.Single(category =>
                category.Audience == audience &&
                category.Slug == slug);
        }

        var products = new List<Product>
        {
            CreateProduct(
                GetCategory(
                    CatalogAudiences.Women,
                    "dresses"),
                "Satin Cowl-Neck Maxi Dress",
                "satin-cowl-neck-maxi-dress",
                "Elegant satin maxi dress with a soft cowl neckline.",
                49.99m,
                "https://images.unsplash.com/photo-1595777457583-95e059d581b8?auto=format&fit=crop&w=720&q=85",
                "AI Pick",
                4.90m,
                143,
                true,
                [
                    ("Champagne", "#E9DDCB"),
                    ("Rose", "#B78D77"),
                    ("Black", "#1F1F1F")
                ],
                [
                    ("S", 14),
                    ("M", 20),
                    ("L", 11)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Women,
                    "dresses"),
                "Floral Tiered Midi Dress",
                "floral-tiered-midi-dress",
                "Lightweight floral midi dress with a flowing tiered skirt.",
                44.99m,
                "https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?auto=format&fit=crop&w=720&q=85",
                "Trending",
                4.80m,
                187,
                true,
                [
                    ("Ivory", "#F2E2D4"),
                    ("Rose", "#BD6D6D"),
                    ("Green", "#314139")
                ],
                [
                    ("S", 17),
                    ("M", 23),
                    ("L", 9)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Women,
                    "tops"),
                "Linen Relaxed Shirt",
                "linen-relaxed-shirt",
                "Relaxed linen shirt designed for effortless everyday styling.",
                39.99m,
                "https://images.unsplash.com/photo-1608234807905-4466023792f5?auto=format&fit=crop&w=720&q=85",
                "AI Pick",
                4.80m,
                128,
                false,
                [
                    ("Beige", "#D8C4A8"),
                    ("Ivory", "#F4F0E9"),
                    ("Black", "#212121")
                ],
                [
                    ("S", 16),
                    ("M", 25),
                    ("L", 13)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Women,
                    "bottoms"),
                "Tailored Wide-Leg Trousers",
                "tailored-wide-leg-trousers",
                "High-waisted tailored trousers with an elegant wide-leg shape.",
                44.99m,
                "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?auto=format&fit=crop&w=720&q=85",
                null,
                4.80m,
                102,
                false,
                [
                    ("Camel", "#D0BDA3"),
                    ("Cream", "#E8E3DA"),
                    ("Charcoal", "#424242")
                ],
                [
                    ("S", 12),
                    ("M", 18),
                    ("L", 10)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Women,
                    "outerwear"),
                "Relaxed Utility Jacket",
                "women-relaxed-utility-jacket",
                "A versatile relaxed jacket for polished everyday layering.",
                54.99m,
                "https://images.unsplash.com/photo-1551028719-00167b16eac5?auto=format&fit=crop&w=720&q=85",
                null,
                4.70m,
                91,
                true,
                [
                    ("Black", "#151515"),
                    ("Olive", "#7E8273"),
                    ("Stone", "#D8D2C8")
                ],
                [
                    ("S", 8),
                    ("M", 15),
                    ("L", 7)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Women,
                    "shoes"),
                "Minimal Leather Sneakers",
                "women-minimal-leather-sneakers",
                "Clean leather sneakers designed for daily comfort.",
                59.99m,
                "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=720&q=85",
                null,
                4.70m,
                87,
                false,
                [
                    ("White", "#F4F2ED"),
                    ("Black", "#181818"),
                    ("Taupe", "#B8A58A")
                ],
                [
                    ("37", 9),
                    ("38", 14),
                    ("39", 17),
                    ("40", 8)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Women,
                    "accessories"),
                "Structured Everyday Tote",
                "women-structured-everyday-tote",
                "A spacious structured tote for work and everyday essentials.",
                49.99m,
                "https://images.unsplash.com/photo-1584917865442-de89df76afd3?auto=format&fit=crop&w=720&q=85",
                "AI Pick",
                4.80m,
                64,
                false,
                [
                    ("Cognac", "#8D5738"),
                    ("Black", "#1A1A1A"),
                    ("Cream", "#E3D5C3")
                ],
                [
                    ("One Size", 24)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Men,
                    "polos"),
                "Piqué Knit Polo",
                "men-pique-knit-polo",
                "A refined piqué polo with a comfortable modern fit.",
                29.99m,
                "https://images.unsplash.com/photo-1617137968427-85924c800a22?auto=format&fit=crop&w=720&q=85",
                "Trending",
                4.70m,
                96,
                true,
                [
                    ("Olive", "#65705C"),
                    ("Stone", "#E7E3DC"),
                    ("Navy", "#1C2734")
                ],
                [
                    ("S", 11),
                    ("M", 24),
                    ("L", 19),
                    ("XL", 8)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Men,
                    "shirts"),
                "Oxford Relaxed Shirt",
                "men-oxford-relaxed-shirt",
                "A clean Oxford shirt with a comfortable relaxed silhouette.",
                39.99m,
                "https://images.unsplash.com/photo-1608234807905-4466023792f5?auto=format&fit=crop&w=720&q=85",
                "AI Pick",
                4.80m,
                118,
                true,
                [
                    ("White", "#F5F3EE"),
                    ("Beige", "#D7C4AA"),
                    ("Black", "#222222")
                ],
                [
                    ("S", 10),
                    ("M", 22),
                    ("L", 17),
                    ("XL", 9)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Men,
                    "trousers"),
                "Tailored Straight Trousers",
                "men-tailored-straight-trousers",
                "Classic tailored trousers with a clean straight-leg cut.",
                44.99m,
                "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?auto=format&fit=crop&w=720&q=85",
                null,
                4.70m,
                82,
                false,
                [
                    ("Camel", "#C7AF91"),
                    ("Charcoal", "#444444"),
                    ("Black", "#151515")
                ],
                [
                    ("30", 8),
                    ("32", 16),
                    ("34", 14),
                    ("36", 7)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Men,
                    "outerwear"),
                "Utility Overshirt Jacket",
                "men-utility-overshirt-jacket",
                "A versatile overshirt jacket for effortless layering.",
                54.99m,
                "https://images.unsplash.com/photo-1551028719-00167b16eac5?auto=format&fit=crop&w=720&q=85",
                "AI Pick",
                4.80m,
                101,
                true,
                [
                    ("Black", "#151515"),
                    ("Olive", "#7E8273"),
                    ("Stone", "#D8D2C8")
                ],
                [
                    ("S", 8),
                    ("M", 18),
                    ("L", 15),
                    ("XL", 6)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Men,
                    "shoes"),
                "Minimal Everyday Sneakers",
                "men-minimal-everyday-sneakers",
                "Minimal sneakers combining everyday comfort and clean styling.",
                59.99m,
                "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=720&q=85",
                null,
                4.70m,
                87,
                false,
                [
                    ("White", "#F4F2ED"),
                    ("Black", "#181818"),
                    ("Taupe", "#B8A58A")
                ],
                [
                    ("40", 9),
                    ("41", 15),
                    ("42", 19),
                    ("43", 12),
                    ("44", 6)
                ]),

            CreateProduct(
                GetCategory(
                    CatalogAudiences.Men,
                    "accessories"),
                "Structured Leather Work Bag",
                "men-structured-leather-work-bag",
                "A structured leather bag for work and daily essentials.",
                69.99m,
                "https://images.unsplash.com/photo-1584917865442-de89df76afd3?auto=format&fit=crop&w=720&q=85",
                null,
                4.80m,
                58,
                false,
                [
                    ("Brown", "#8D5738"),
                    ("Black", "#1A1A1A"),
                    ("Tan", "#C39A70")
                ],
                [
                    ("One Size", 18)
                ]),
        };

        var existingProducts =
            await dbContext.Products
                .AsNoTracking()
                .Select(product => new
                {
                    product.CategoryId,
                    product.Slug
                })
                .ToListAsync(cancellationToken);

        var existingKeys = existingProducts
            .Select(product =>
                CreateKey(
                    product.CategoryId,
                    product.Slug))
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var missingProducts = products
            .Where(product =>
                !existingKeys.Contains(
                    CreateKey(
                        product.CategoryId,
                        product.Slug)))
            .ToList();

        if (missingProducts.Count == 0)
        {
            return;
        }

        dbContext.Products.AddRange(
            missingProducts);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static Product CreateProduct(
        Category category,
        string name,
        string slug,
        string description,
        decimal price,
        string imageUrl,
        string? badge,
        decimal rating,
        int reviewCount,
        bool isNew,
        (string Name, string HexCode)[] colors,
        (string Name, int StockQuantity)[] sizes)
    {
        return new Product
        {
            CategoryId = category.Id,
            Name = name,
            Slug = slug,
            Description = description,
            Price = price,
            ImageUrl = imageUrl,
            Badge = badge,
            Rating = rating,
            ReviewCount = reviewCount,
            IsNew = isNew,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,

            Colors = colors
                .Select(color =>
                    new ProductColor
                    {
                        Name = color.Name,
                        HexCode = color.HexCode
                    })
                .ToList(),

            Sizes = sizes
                .Select(size =>
                    new ProductSize
                    {
                        Name = size.Name,
                        StockQuantity =
                            size.StockQuantity
                    })
                .ToList()
        };
    }

    private static string CreateKey(
        Guid categoryId,
        string slug)
    {
        return $"{categoryId}:{slug}";
    }
}