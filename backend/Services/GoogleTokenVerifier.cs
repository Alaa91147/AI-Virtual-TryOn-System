using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace VirtualTryOn.Api.Services;

public sealed class GoogleTokenVerifier(IOptions<GoogleAuthOptions> googleAuthOptions)
{
    private const string MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";

    private readonly GoogleAuthOptions _googleAuthOptions = googleAuthOptions.Value;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager = new(
        MetadataAddress,
        new OpenIdConnectConfigurationRetriever());

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId);

    public async Task<GoogleAccount?> VerifyAsync(string credential, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new GoogleAuthNotConfiguredException();
        }

        try
        {
            var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
            var principal = ValidateToken(credential, configuration, out _);

            return CreateGoogleAccount(principal);
        }
        catch (SecurityTokenSignatureKeyNotFoundException)
        {
            _configurationManager.RequestRefresh();
            var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
            var principal = ValidateToken(credential, configuration, out _);

            return CreateGoogleAccount(principal);
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is SecurityTokenException)
        {
            return null;
        }
    }

    private ClaimsPrincipal ValidateToken(
        string credential,
        OpenIdConnectConfiguration configuration,
        out SecurityToken validatedToken)
    {
        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        return handler.ValidateToken(
            credential,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
                ValidateAudience = true,
                ValidAudience = _googleAuthOptions.ClientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                NameClaimType = "name"
            },
            out validatedToken);
    }

    private static GoogleAccount? CreateGoogleAccount(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
        var name = principal.FindFirstValue("name");
        var emailVerifiedValue = principal.FindFirstValue("email_verified");

        var emailVerified = bool.TryParse(emailVerifiedValue, out var verified) && verified;

        if (string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(email) ||
            !emailVerified)
        {
            return null;
        }

        return new GoogleAccount(subject, email, name, emailVerified);
    }
}

public sealed record GoogleAccount(
    string Subject,
    string Email,
    string? Name,
    bool EmailVerified);
