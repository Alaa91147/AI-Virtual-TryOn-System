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
    DateTimeOffset? LastOrderAt,
    bool IsSuspended,
    string? SuspensionReason,
    DateTimeOffset? SuspendedUntil,
    string? AdminNotes);

public sealed record UpdateCustomerRoleRequest(
    [Required] string Role);

public sealed record SendCustomerNotificationRequest(
    [Required, StringLength(100, MinimumLength = 2)]
    string Title,

    [Required, StringLength(500, MinimumLength = 2)]
    string Message);

public sealed class UpdateCustomerAdministrationRequest :
    IValidatableObject
{
    public bool IsSuspended { get; set; }

    [StringLength(500)]
    public string? SuspensionReason { get; set; }

    public DateTimeOffset? SuspendedUntil { get; set; }

    [StringLength(2000)]
    public string? AdminNotes { get; set; }

    public bool RevokeSessions { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (
            IsSuspended &&
            string.IsNullOrWhiteSpace(SuspensionReason)
        )
        {
            yield return new ValidationResult(
                "A suspension reason is required.",
                [nameof(SuspensionReason)]);
        }

        if (
            IsSuspended &&
            SuspendedUntil.HasValue &&
            SuspendedUntil <= DateTimeOffset.UtcNow
        )
        {
            yield return new ValidationResult(
                "Suspension expiry must be in the future.",
                [nameof(SuspendedUntil)]);
        }
    }
}
