using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.DTOs.Auth;
using VirtualTryOn.Api.Responses;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
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
        var response = await authService.LoginAsync(request, cancellationToken);
        if (response is null)
        {
            return Unauthorized(ApiError.Create(
                "Invalid email or password.",
                "Invalid email or password."));
        }

        return Ok(response);
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
}
