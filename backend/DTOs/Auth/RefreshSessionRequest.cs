using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Auth;

public sealed class RefreshSessionRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
