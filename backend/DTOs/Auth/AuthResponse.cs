namespace VirtualTryOn.Api.DTOs.Auth;

public sealed record AuthResponse(
    string Token,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserProfileResponse User);
