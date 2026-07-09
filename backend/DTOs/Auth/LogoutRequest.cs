namespace VirtualTryOn.Api.DTOs.Auth;

public sealed class LogoutRequest
{
    public string? RefreshToken { get; set; }
}
