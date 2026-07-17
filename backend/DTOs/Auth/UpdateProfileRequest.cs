using System.ComponentModel.DataAnnotations;
using VirtualTryOn.Api.Constants;

namespace VirtualTryOn.Api.DTOs.Auth;

public sealed class UpdateProfileRequest : IValidatableObject
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 120 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gender is required.")]
    public string Gender { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(30, ErrorMessage = "Phone number cannot be longer than 30 characters.")]
    public string? PhoneNumber { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public string? FullBodyPhotoUrl { get; set; }

    public string? UpperBodyPhotoUrl { get; set; }

    public string? LowerBodyPhotoUrl { get; set; }

    public string? FacePhotoUrl { get; set; }

    [Range(50, 260, ErrorMessage = "Height must be between 50 and 260 cm.")]
    public decimal? HeightCm { get; set; }

    [Range(20, 300, ErrorMessage = "Weight must be between 20 and 300 kg.")]
    public decimal? WeightKg { get; set; }

    [StringLength(20, ErrorMessage = "Preferred size cannot be longer than 20 characters.")]
    public string? PreferredSize { get; set; }

    [StringLength(40, ErrorMessage = "Body shape cannot be longer than 40 characters.")]
    public string? BodyShape { get; set; }

    [StringLength(20, ErrorMessage = "Shoe size cannot be longer than 20 characters.")]
    public string? ShoeSize { get; set; }

    [StringLength(20, ErrorMessage = "Top size cannot be longer than 20 characters.")]
    public string? TopSize { get; set; }

    [StringLength(20, ErrorMessage = "Bottom size cannot be longer than 20 characters.")]
    public string? BottomSize { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var normalizedGender = (Gender ?? string.Empty).Trim().ToLowerInvariant();

        if (!GenderOptions.All.Contains(normalizedGender))
        {
            yield return new ValidationResult(
                "Gender must be male, female, or other.",
                [nameof(Gender)]);
        }

        if (DateOfBirth is not null && DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            yield return new ValidationResult(
                "Date of birth cannot be in the future.",
                [nameof(DateOfBirth)]);
        }
    }
}
