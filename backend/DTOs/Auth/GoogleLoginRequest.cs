using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Auth;

public sealed class GoogleLoginRequest
{
    [Required(ErrorMessage = "Google credential is required.")]
    public string Credential { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
