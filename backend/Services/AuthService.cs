using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Auth;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public class AuthService(
    AppDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    JwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions,
    GoogleTokenVerifier googleTokenVerifier,
    IEmailSender emailSender,
    IOptions<AppUrlOptions> appUrlOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly AppUrlOptions _appUrlOptions = appUrlOptions.Value;
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(6);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);

        var emailExists = await dbContext.Users
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new DuplicateEmailException();
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            Gender = request.Gender.Trim().ToLowerInvariant(),
            Role = UserRoles.Customer,
            DateOfBirth = request.DateOfBirth,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);

        var response = CreateAuthResponse(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = response.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var response = CreateAuthResponse(user);
        var refreshTokenDays = request.RememberMe
            ? _jwtOptions.RememberMeRefreshTokenDays
            : _jwtOptions.RefreshTokenDays;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = response.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshTokenDays)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse?> GoogleLoginAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!googleTokenVerifier.IsConfigured)
        {
            throw new GoogleAuthNotConfiguredException();
        }

        var googleAccount = await googleTokenVerifier.VerifyAsync(request.Credential, cancellationToken);
        if (googleAccount is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var email = googleAccount.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);

        var user = await dbContext.Users
            .SingleOrDefaultAsync(account => account.GoogleSubject == googleAccount.Subject, cancellationToken);

        if (user is null)
        {
            user = await dbContext.Users
                .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    FullName = GetGoogleDisplayName(googleAccount),
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    GoogleSubject = googleAccount.Subject,
                    Gender = GenderOptions.Other,
                    Role = UserRoles.Customer,
                    IsEmailVerified = true,
                    CreatedAt = now
                };

                dbContext.Users.Add(user);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(user.GoogleSubject) &&
                    user.GoogleSubject != googleAccount.Subject)
                {
                    return null;
                }

                user.GoogleSubject = googleAccount.Subject;
                user.IsEmailVerified = true;
                user.UpdatedAt = now;
            }
        }
        else if (!user.IsEmailVerified)
        {
            user.IsEmailVerified = true;
            user.UpdatedAt = now;
        }

        var response = CreateAuthResponse(user);
        var refreshTokenDays = request.RememberMe
            ? _jwtOptions.RememberMeRefreshTokenDays
            : _jwtOptions.RefreshTokenDays;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = response.RefreshToken,
            ExpiresAt = now.AddDays(refreshTokenDays)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<string?> RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var token = CreateSecureToken();
        var tokenHash = HashToken(token);

        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.Add(PasswordResetTokenLifetime)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var resetLink = CreatePasswordResetLink(token);
        await emailSender.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);
        return resetLink;
    }

    public async Task<bool> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.Token);
        var now = DateTimeOffset.UtcNow;

        var resetToken = await dbContext.PasswordResetTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null ||
            resetToken.UsedAt is not null ||
            resetToken.ExpiresAt <= now)
        {
            return false;
        }

        resetToken.User.PasswordHash = passwordHasher.HashPassword(resetToken.User, request.Password);
        resetToken.User.UpdatedAt = now;

        var activeResetTokens = await dbContext.PasswordResetTokens
            .Where(token =>
                token.UserId == resetToken.UserId &&
                token.UsedAt == null &&
                token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var activeResetToken in activeResetTokens)
        {
            activeResetToken.UsedAt = now;
        }

        var activeRefreshTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == resetToken.UserId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserProfileResponse?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return null;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Id == userId.Value, cancellationToken);

        return user is null ? null : ToProfileResponse(user);
    }

    public async Task<UserProfileResponse?> UpdateCurrentUserAsync(
        ClaimsPrincipal principal,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return null;
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(account => account.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.FullName = request.FullName.Trim();
        user.Gender = request.Gender.Trim().ToLowerInvariant();
        user.DateOfBirth = request.DateOfBirth;
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.ProfilePhotoUrl = NormalizeOptional(request.ProfilePhotoUrl);
        user.FullBodyPhotoUrl = NormalizeOptional(request.FullBodyPhotoUrl);
        user.UpperBodyPhotoUrl = NormalizeOptional(request.UpperBodyPhotoUrl);
        user.LowerBodyPhotoUrl = NormalizeOptional(request.LowerBodyPhotoUrl);
        user.FacePhotoUrl = NormalizeOptional(request.FacePhotoUrl);
        user.HeightCm = request.HeightCm;
        user.WeightKg = request.WeightKg;
        user.PreferredSize = NormalizeOptional(request.PreferredSize);
        user.BodyShape = NormalizeOptional(request.BodyShape);
        user.ShoeSize = NormalizeOptional(request.ShoeSize);
        user.TopSize = NormalizeOptional(request.TopSize);
        user.BottomSize = NormalizeOptional(request.BottomSize);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToProfileResponse(user);
    }

    public async Task LogoutAsync(ClaimsPrincipal principal, string? refreshToken, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return;
        }

        var activeTokens = dbContext.RefreshTokens
            .Where(token => token.UserId == userId.Value && token.RevokedAt == null);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            activeTokens = activeTokens.Where(token => token.Token == refreshToken);
        }

        var tokens = await activeTokens.ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = jwtTokenService.CreateAccessToken(user, out var expiresAt);
        var refreshToken = jwtTokenService.CreateRefreshToken();

        return new AuthResponse(
            token,
            refreshToken,
            expiresAt,
            ToProfileResponse(user));
    }

    private static UserProfileResponse ToProfileResponse(User user)
    {
        return new UserProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Gender,
            user.PhoneNumber,
            user.ProfilePhotoUrl,
            user.FullBodyPhotoUrl,
            user.UpperBodyPhotoUrl,
            user.LowerBodyPhotoUrl,
            user.FacePhotoUrl,
            user.HeightCm,
            user.WeightKg,
            user.PreferredSize,
            user.BodyShape,
            user.ShoeSize,
            user.TopSize,
            user.BottomSize,
            user.Role,
            user.IsEmailVerified,
            user.DateOfBirth,
            user.CreatedAt,
            user.UpdatedAt);
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string CreatePasswordResetLink(string token)
    {
        var baseUrl = _appUrlOptions.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/auth/reset-password?token={Uri.EscapeDataString(token)}";
    }

    private static string CreateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string GetGoogleDisplayName(GoogleAccount googleAccount)
    {
        if (!string.IsNullOrWhiteSpace(googleAccount.Name))
        {
            return googleAccount.Name.Trim();
        }

        return googleAccount.Email.Trim();
    }
}

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
        : base("Email already exists.")
    {
    }
}

public sealed class GoogleAuthNotConfiguredException : Exception
{
    public GoogleAuthNotConfiguredException()
        : base("Google sign-in is not configured.")
    {
    }
}
