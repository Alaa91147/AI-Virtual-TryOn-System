using VirtualTryOn.Api.Constants;

namespace VirtualTryOn.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? GoogleSubject { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public string? FullBodyPhotoUrl { get; set; }

    public string? UpperBodyPhotoUrl { get; set; }

    public string? LowerBodyPhotoUrl { get; set; }

    public string? FacePhotoUrl { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public string? PreferredSize { get; set; }

    public string? BodyShape { get; set; }

    public string? ShoeSize { get; set; }

    public string? TopSize { get; set; }

    public string? BottomSize { get; set; }

    public string Role { get; set; } = UserRoles.Customer;

    public bool IsEmailVerified { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
}
