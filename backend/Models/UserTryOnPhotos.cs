namespace VirtualTryOn.Api.Models;

public class UserTryOnPhotos
{
    public Guid UserId { get; set; }

    public string? FullBodyPhotoUrl { get; set; }

    public string? UpperBodyPhotoUrl { get; set; }

    public string? LowerBodyPhotoUrl { get; set; }

    public string? FacePhotoUrl { get; set; }

    public User User { get; set; } = null!;
}
