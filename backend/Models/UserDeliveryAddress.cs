namespace VirtualTryOn.Api.Models;

public class UserDeliveryAddress
{
    public Guid UserId { get; set; }

    public string? Country { get; set; }

    public string? City { get; set; }

    public string? Street { get; set; }

    public string? Building { get; set; }

    public string? PhoneNumber { get; set; }

    public User User { get; set; } = null!;
}
