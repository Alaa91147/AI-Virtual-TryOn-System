using System.ComponentModel.DataAnnotations;

namespace VirtualTryOn.Api.DTOs.Admin;

public sealed class ManualEmailVerificationRequest
{
    [Required]
    [MinLength(5)]
    [MaxLength(500)]
    public string Reason { get; set; } =
        string.Empty;
}
