using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Admin;

public sealed class CreatePromotionRequest : IValidatableObject
{
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Range(1, 90)] public decimal DiscountPercentage { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    [MinLength(1)] public List<Guid> ProductIds { get; set; } = [];
    public bool SendEmail { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndsAt <= StartsAt)
            yield return new ValidationResult("The end date must be after the start date.", [nameof(EndsAt)]);
    }
}

public sealed record PromotionResponse(
    Guid Id, string Name, decimal DiscountPercentage,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    bool IsActive, bool EmailSent, int ProductCount,
    int EmailSuccessCount, int EmailFailureCount,
    IReadOnlyList<Guid> ProductIds);
