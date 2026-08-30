namespace VirtualTryOn.Api.Services;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(
        string email,
        string customerName,
        string verificationLink,
        CancellationToken cancellationToken);
    Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken);
    Task SendPromotionEmailAsync(
        string email, string customerName, string promotionName,
        decimal discountPercentage, DateTimeOffset endsAt,
        CancellationToken cancellationToken);
}

