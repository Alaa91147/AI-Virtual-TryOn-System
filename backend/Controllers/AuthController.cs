using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using VirtualTryOn.Api.DTOs.Auth;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.RegisterAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (DuplicateEmailException)
        {
            return Conflict(ApiError.Create("Email already exists.", "Email already exists."));
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (response is null)
        {
            return Unauthorized(ApiError.Create(
                "Invalid email or password.",
                "Invalid email or password."));
        }

        return Ok(response);
    }

    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.GoogleLoginAsync(request, cancellationToken);
            if (response is null)
            {
                return Unauthorized(ApiError.Create(
                    "Could not verify Google sign-in.",
                    "Could not verify Google sign-in."));
            }

            return Ok(response);
        }
        catch (GoogleAuthNotConfiguredException)
        {
            return BadRequest(ApiError.Create(
                "Google sign-in is not configured.",
                "Set GoogleAuth:ClientId on the backend."));
        }
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiMessage>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var resetLink = await authService.RequestPasswordResetAsync(request, cancellationToken);
        var developmentResetLink = environment.IsDevelopment() ? resetLink : null;

        return Ok(new ApiMessage("If this email exists, we sent a reset link.", developmentResetLink));
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiMessage>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var reset = await authService.ResetPasswordAsync(request, cancellationToken);
        if (!reset)
        {
            return BadRequest(ApiError.Create(
                "Invalid or expired reset link.",
                "This reset link is invalid, expired, or already used."));
        }

        return Ok(new ApiMessage("Your password has been reset successfully."));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiMessage>> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(User, request.RefreshToken, cancellationToken);
        return Ok(new ApiMessage("Signed out successfully."));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RefreshSessionAsync(request.RefreshToken, cancellationToken);
        if (response is null)
        {
            return Unauthorized(ApiError.Create(
                "Your session has expired.",
                "Please sign in again."));
        }

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var profile = await authService.GetCurrentUserAsync(User, cancellationToken);
        if (profile is null)
        {
            return Unauthorized(ApiError.Create(
                "You need to sign in again.",
                "You need to sign in again."));
        }

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMe(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await authService.UpdateCurrentUserAsync(User, request, cancellationToken);
        if (profile is null)
        {
            return Unauthorized(ApiError.Create(
                "You need to sign in again.",
                "You need to sign in again."));
        }

        return Ok(profile);
    }
    [Authorize]
[HttpPut("shopping-preference")]
[ProducesResponseType(
    typeof(UserProfileResponse),
    StatusCodes.Status200OK)]
[ProducesResponseType(
    typeof(ApiError),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ApiError),
    StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<UserProfileResponse>>
    UpdateShoppingPreference(
        UpdateShoppingPreferenceRequest request,
        CancellationToken cancellationToken)
{
    var profile =
        await authService
            .UpdateShoppingPreferenceAsync(
                User,
                request.Preference,
                cancellationToken);

    if (profile is null)
    {
        return Unauthorized(
            ApiError.Create(
                "You need to sign in again.",
                "You need to sign in again."));
    }

    return Ok(profile);
}

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiMessage>>
        VerifyEmail(
            VerifyEmailRequest request,
            CancellationToken cancellationToken)
    {
        var verified =
            await authService.VerifyEmailAsync(
                request.Token,
                cancellationToken);

        if (!verified)
        {
            return BadRequest(
                ApiError.Create(
                    "Invalid or expired verification link.",
                    "Request a new verification email."));
        }

        return Ok(
            new ApiMessage(
                "Your email was verified successfully."));
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<ActionResult<ApiMessage>>
        ResendVerification(
            ResendVerificationRequest request,
            CancellationToken cancellationToken)
    {
        await authService.RequestVerificationAsync(
            request.Email,
            cancellationToken);

        return Ok(
            new ApiMessage(
                "If this account requires verification, a new email was sent."));
    }}


