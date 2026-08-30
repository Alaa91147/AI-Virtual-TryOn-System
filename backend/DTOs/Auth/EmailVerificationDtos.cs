using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Auth;

public sealed class VerifyEmailRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public sealed class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
