namespace VirtualTryOn.Api.DTOs.Auth;

public sealed record UserProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string Gender,
    string Role,
    bool IsEmailVerified,
    DateOnly? DateOfBirth,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
