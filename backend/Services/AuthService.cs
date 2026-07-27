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
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = UserRoles.Customer,
            CreatedAt = DateTimeOffset.UtcNow,
            Profile = new UserProfile
            {
                FullName = request.FullName.Trim(),
                Gender = request.Gender.Trim().ToLowerInvariant(),
                DateOfBirth = request.DateOfBirth
            },
            FitProfile = new UserFitProfile(),
            TryOnPhotos = new UserTryOnPhotos()
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

        var user = await UsersWithProfileSections()
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

        var user = await UsersWithProfileSections()
            .SingleOrDefaultAsync(account => account.GoogleSubject == googleAccount.Subject, cancellationToken);

        if (user is null)
        {
            user = await UsersWithProfileSections()
                .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    GoogleSubject = googleAccount.Subject,
                    Role = UserRoles.Customer,
                    IsEmailVerified = true,
                    CreatedAt = now,
                    Profile = new UserProfile
                    {
                        FullName = GetGoogleDisplayName(googleAccount),
                        Gender = GenderOptions.Other
                    },
                    FitProfile = new UserFitProfile(),
                    TryOnPhotos = new UserTryOnPhotos()
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
                EnsureProfile(user);
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

        var user = await UsersWithProfileSections()
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

        var user = await UsersWithProfileSections()
            .SingleOrDefaultAsync(account => account.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var profile = EnsureProfile(user);
        var address = EnsureDeliveryAddress(user);
        var photos = EnsureTryOnPhotos(user);
        var fitProfile = EnsureFitProfile(user);

        profile.FullName = request.FullName.Trim();
        profile.Gender = request.Gender.Trim().ToLowerInvariant();
        profile.DateOfBirth = request.DateOfBirth;
        profile.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        profile.ProfilePhotoUrl = NormalizeOptional(request.ProfilePhotoUrl);

        address.Country = NormalizeOptional(request.DeliveryCountry);
        address.City = NormalizeOptional(request.DeliveryCity);
        address.Street = NormalizeOptional(request.DeliveryStreet);
        address.Building = NormalizeOptional(request.DeliveryBuilding);
        address.PhoneNumber = NormalizeOptional(request.DeliveryPhoneNumber);

        photos.FullBodyPhotoUrl = NormalizeOptional(request.FullBodyPhotoUrl);
        photos.UpperBodyPhotoUrl = NormalizeOptional(request.UpperBodyPhotoUrl);
        photos.LowerBodyPhotoUrl = NormalizeOptional(request.LowerBodyPhotoUrl);
        photos.FacePhotoUrl = NormalizeOptional(request.FacePhotoUrl);

        fitProfile.HeightCm = request.HeightCm;
        fitProfile.WeightKg = request.WeightKg;
        fitProfile.PreferredSize = NormalizeOptional(request.PreferredSize);
        fitProfile.BodyShape = NormalizeOptional(request.BodyShape);
        fitProfile.ShoeSize = NormalizeOptional(request.ShoeSize);
        fitProfile.TopSize = NormalizeOptional(request.TopSize);
        fitProfile.BottomSize = NormalizeOptional(request.BottomSize);
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
        EnsureProfile(user);

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
        var profile = user.Profile;
        var address = user.DeliveryAddress;
        var photos = user.TryOnPhotos;
        var fitProfile = user.FitProfile;

        return new UserProfileResponse(
            user.Id,
            profile?.FullName ?? user.Email,
            user.Email,
            profile?.Gender ?? GenderOptions.Other,
user.ShoppingPreference,
profile?.PhoneNumber,
            address?.Country,
            address?.City,
            address?.Street,
            address?.Building,
            address?.PhoneNumber,
            profile?.ProfilePhotoUrl,
            photos?.FullBodyPhotoUrl,
            photos?.UpperBodyPhotoUrl,
            photos?.LowerBodyPhotoUrl,
            photos?.FacePhotoUrl,
            fitProfile?.HeightCm,
            fitProfile?.WeightKg,
            fitProfile?.PreferredSize,
            fitProfile?.BodyShape,
            fitProfile?.ShoeSize,
            fitProfile?.TopSize,
            fitProfile?.BottomSize,
            user.Role,
            user.IsEmailVerified,
            profile?.DateOfBirth,
            user.CreatedAt,
            user.UpdatedAt);
    }

    private IQueryable<User> UsersWithProfileSections()
    {
        return dbContext.Users
            .Include(user => user.Profile)
            .Include(user => user.FitProfile)
            .Include(user => user.TryOnPhotos)
            .Include(user => user.DeliveryAddress);
    }
public async Task<UserProfileResponse?>
    UpdateShoppingPreferenceAsync(
        ClaimsPrincipal principal,
        string preference,
        CancellationToken cancellationToken)
{
    var userId = GetUserId(principal);

    if (userId is null)
    {
        return null;
    }

    var normalizedPreference =
        preference.Trim().ToLowerInvariant();

    if (!ShoppingPreferences.All.Contains(
            normalizedPreference))
    {
        throw new ArgumentException(
            "Invalid shopping preference.",
            nameof(preference));
    }

    var user = await UsersWithProfileSections()
        .SingleOrDefaultAsync(
            account =>
                account.Id == userId.Value,
            cancellationToken);

    if (user is null)
    {
        return null;
    }

    user.ShoppingPreference =
        normalizedPreference;

    user.UpdatedAt =
        DateTimeOffset.UtcNow;

    await dbContext.SaveChangesAsync(
        cancellationToken);

    return ToProfileResponse(user);
}
    private static UserProfile EnsureProfile(User user)
    {
        if (user.Profile is null)
        {
            user.Profile = new UserProfile
            {
                UserId = user.Id,
                FullName = user.Email,
                Gender = GenderOptions.Other
            };
        }

        return user.Profile;
    }

    private static UserFitProfile EnsureFitProfile(User user)
    {
        user.FitProfile ??= new UserFitProfile
        {
            UserId = user.Id
        };

        return user.FitProfile;
    }

    private static UserTryOnPhotos EnsureTryOnPhotos(User user)
    {
        user.TryOnPhotos ??= new UserTryOnPhotos
        {
            UserId = user.Id
        };

        return user.TryOnPhotos;
    }

    private static UserDeliveryAddress EnsureDeliveryAddress(User user)
    {
        user.DeliveryAddress ??= new UserDeliveryAddress
        {
            UserId = user.Id
        };

        return user.DeliveryAddress;
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
