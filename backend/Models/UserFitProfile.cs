namespace VirtualTryOn.Api.Models;

public class UserFitProfile
{
    public Guid UserId { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public string? PreferredSize { get; set; }

    public string? BodyShape { get; set; }

    public string? ShoeSize { get; set; }

    public string? TopSize { get; set; }

    public string? BottomSize { get; set; }

    public User User { get; set; } = null!;
}
