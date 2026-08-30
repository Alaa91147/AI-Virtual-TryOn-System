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

        if (EndsAt <= DateTimeOffset.UtcNow)
            yield return new ValidationResult("The promotion must end in the future.", [nameof(EndsAt)]);

        if (EndsAt - StartsAt > TimeSpan.FromDays(366))
            yield return new ValidationResult("A promotion cannot run for more than one year.", [nameof(EndsAt)]);

        if (ProductIds.Count != ProductIds.Distinct().Count())
            yield return new ValidationResult("Each product can only be selected once.", [nameof(ProductIds)]);
    }
}

public sealed record PromotionResponse(
    Guid Id, string Name, decimal DiscountPercentage,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    bool IsActive, bool EmailSent, int ProductCount,
    int EmailSuccessCount, int EmailFailureCount,
    IReadOnlyList<Guid> ProductIds);

public sealed class UpdatePromotionRequest : IValidatableObject
{
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Range(1, 90)] public decimal DiscountPercentage { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    [MinLength(1)] public List<Guid> ProductIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndsAt <= StartsAt)
            yield return new ValidationResult("The end date must be after the start date.", [nameof(EndsAt)]);
        if (EndsAt <= DateTimeOffset.UtcNow)
            yield return new ValidationResult("The promotion must end in the future.", [nameof(EndsAt)]);
        if (ProductIds.Count != ProductIds.Distinct().Count())
            yield return new ValidationResult("Each product can only be selected once.", [nameof(ProductIds)]);
    }
}
