using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Admin;

public sealed record AdminCustomerResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsEmailVerified,
    bool UsesGoogle,
    string? Gender,
    string? ShoppingPreference,
    string? PhoneNumber,
    string? Address,
    DateTimeOffset CreatedAt,
    int OrderCount,
    decimal TotalSpent,
    DateTimeOffset? LastOrderAt);

public sealed record UpdateCustomerRoleRequest(
    [Required] string Role);

public sealed record SendCustomerNotificationRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Title,
    [Required, StringLength(500, MinimumLength = 2)] string Message);
