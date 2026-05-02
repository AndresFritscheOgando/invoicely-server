using System.Security.Claims;
using FluentValidation;
using Invoicely.Core.DTOs;
using Invoicely.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Invoicely.Api.Controllers;

/// <summary>Authentication and user profile management.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<UpdateProfileRequest> updateProfileValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator,
    IConfiguration configuration) : ControllerBase
{
    private void SetAuthCookie(string token)
    {
        var expiry = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;
        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(expiry)
        });
    }
    private Guid? CurrentUserId
    {
        get
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }

    /// <summary>Register a new user account. Sets an httpOnly auth cookie.</summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await authService.RegisterAsync(request, ct);
            SetAuthCookie(result.Token);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Authenticate with email and password. Sets an httpOnly auth cookie.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await authService.LoginAsync(request, ct);
            SetAuthCookie(result.Token);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid credentials." });
        }
    }

    /// <summary>Clear the auth cookie and end the session.</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    /// <summary>Return the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (CurrentUserId is not Guid userId)
            return Unauthorized();

        try
        {
            var user = await authService.GetMeAsync(userId, ct);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Update the current user's name and email.</summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        if (CurrentUserId is not Guid userId)
            return Unauthorized();

        var validation = await updateProfileValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var user = await authService.UpdateProfileAsync(userId, request, ct);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Change the current user's password.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        if (CurrentUserId is not Guid userId)
            return Unauthorized();

        var validation = await changePasswordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            await authService.ChangePasswordAsync(userId, request, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
