using VirtualTryOn.Api.Constants;

namespace VirtualTryOn.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string Role { get; set; } = UserRoles.Customer;

    public bool IsEmailVerified { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
