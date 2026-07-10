using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace VirtualTryOn.Api.Services;

public sealed class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.SmtpHost) ||
            string.IsNullOrWhiteSpace(_emailOptions.From))
        {
            logger.LogWarning(
                "SMTP email is not configured. Password reset link for {Email}: {ResetLink}",
                email,
                resetLink);
            return;
        }

        var encodedResetLink = WebUtility.HtmlEncode(resetLink);
        var htmlBody = $$"""
                       <!doctype html>
                       <html lang="en">
                       <head>
                         <meta charset="utf-8">
                         <meta name="viewport" content="width=device-width, initial-scale=1">
                         <title>Reset your password</title>
                       </head>
                       <body style="margin:0;background:#f4f1ec;font-family:Arial,Helvetica,sans-serif;color:#191715;">
                         <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">Use the secure button to reset your AI Virtual Try-On password. The link expires in 6 minutes.</div>
                         <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f1ec;padding:34px 16px;">
                           <tr>
                             <td align="center">
                               <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border:1px solid #ded8ce;border-radius:12px;overflow:hidden;box-shadow:0 18px 46px rgba(25,23,21,.10);">
                                 <tr>
                                   <td style="height:6px;background:#9a7b4f;"></td>
                                 </tr>
                                 <tr>
                                   <td style="padding:34px 34px 18px;">
                                     <p style="margin:0 0 18px;color:#191715;font-size:19px;font-weight:800;line-height:1.25;">AI Virtual Try-On</p>
                                     <p style="margin:0 0 10px;color:#9a7b4f;font-size:12px;font-weight:800;letter-spacing:.12em;text-transform:uppercase;">Password reset</p>
                                     <h1 style="margin:0 0 14px;color:#191715;font-size:30px;line-height:1.2;font-weight:800;">Reset your password</h1>
                                     <p style="margin:0 0 26px;color:#68625a;font-size:16px;line-height:1.6;">We received a request to reset the password for your account. Click the button below to create a new password.</p>
                                     <p style="margin:0 0 28px;">
                                       <a href="{{encodedResetLink}}" style="display:inline-block;background:#191715;color:#ffffff;text-decoration:none;font-size:16px;font-weight:800;padding:15px 28px;border-radius:8px;">Reset Password</a>
                                     </p>
                                     <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#faf8f4;border:1px solid #e6dfd4;border-radius:8px;margin:0 0 22px;">
                                       <tr>
                                         <td style="padding:15px 16px;color:#5f594f;font-size:14px;line-height:1.55;">
                                           This secure reset button expires in <strong style="color:#191715;">6 minutes</strong>. If you did not request a password reset, you can safely ignore this email.
                                         </td>
                                       </tr>
                                     </table>
                                     <p style="margin:0;color:#7a7369;font-size:13px;line-height:1.55;">If the button does not work, copy and paste this link into your browser:</p>
                                     <p style="margin:8px 0 0;color:#7a7369;font-size:12px;line-height:1.55;word-break:break-all;">
                                       <a href="{{encodedResetLink}}" style="color:#735a35;text-decoration:underline;">{{encodedResetLink}}</a>
                                     </p>
                                   </td>
                                 </tr>
                                 <tr>
                                   <td style="padding:18px 34px 30px;color:#9a948b;font-size:12px;line-height:1.5;border-top:1px solid #eee8df;">
                                     AI Virtual Try-On sent this email to help protect your account.
                                   </td>
                                 </tr>
                               </table>
                             </td>
                           </tr>
                         </table>
                       </body>
                       </html>
                       """;

        using var message = new MailMessage
        {
            From = new MailAddress(_emailOptions.From, _emailOptions.FromName),
            Subject = "Reset your AI Virtual Try-On password",
            Body = htmlBody,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Priority = MailPriority.Normal
        };

        message.To.Add(email);
        message.Headers.Add("X-Auto-Response-Suppress", "All");

        using var smtpClient = new SmtpClient(_emailOptions.SmtpHost, _emailOptions.SmtpPort)
        {
            EnableSsl = _emailOptions.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_emailOptions.SmtpUsername))
        {
            smtpClient.Credentials = new NetworkCredential(
                _emailOptions.SmtpUsername,
                _emailOptions.SmtpPassword);
        }

        await smtpClient.SendMailAsync(message, cancellationToken);

        logger.LogWarning("Password reset email sent to {Email}.", email);
    }
}
