namespace VirtualTryOn.Api.Models;

public class UserProfile
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public User User { get; set; } = null!;
}
