using System.ComponentModel.DataAnnotations;
using VirtualTryOn.Api.Constants;

namespace VirtualTryOn.Api.DTOs.Auth;

public sealed class RegisterRequest : IValidatableObject
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 120 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(254, ErrorMessage = "Email cannot be longer than 254 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gender is required.")]
    public string Gender { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public bool AcceptTerms { get; set; }

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

        if (!AcceptTerms)
        {
            yield return new ValidationResult(
                "You must accept the terms and conditions.",
                [nameof(AcceptTerms)]);
        }
    }
}
