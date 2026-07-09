namespace VirtualTryOn.Api.Services;

public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken);
}
