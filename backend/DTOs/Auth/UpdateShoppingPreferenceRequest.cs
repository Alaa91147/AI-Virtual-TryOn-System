using System.ComponentModel.DataAnnotations;
using VirtualTryOn.Api.Constants;

namespace VirtualTryOn.Api.DTOs.Auth;

public sealed class UpdateShoppingPreferenceRequest :
    IValidatableObject
{
    [Required(
        ErrorMessage =
            "Shopping preference is required.")]
    public string Preference { get; set; } =
        string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var normalizedPreference =
            (Preference ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

        if (!ShoppingPreferences.All.Contains(
                normalizedPreference))
        {
            yield return new ValidationResult(
                "Shopping preference must be women, men, or both.",
                [nameof(Preference)]);
        }
    }
}