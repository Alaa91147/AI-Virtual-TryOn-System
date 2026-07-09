using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Auth;

public sealed class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(254, ErrorMessage = "Email cannot be longer than 254 characters.")]
    public string Email { get; set; } = string.Empty;
}
