using System.Security.Claims;
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
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

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
}

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
        : base("Email already exists.")
    {
    }
}
