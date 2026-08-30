using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Admin;

public sealed record AdminCategoryDetailResponse(
    Guid Id, string Name, string Slug, string Audience, string? ImageUrl,
    int DisplayOrder, bool IsActive, int ProductCount);

public sealed class SaveAdminCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = string.Empty;

    [Required, RegularExpression("^(women|men|unisex)$")]
    public string Audience { get; set; } = string.Empty;

    [StringLength(2048)]
    public string? ImageUrl { get; set; }

    [Range(0, 10000)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
