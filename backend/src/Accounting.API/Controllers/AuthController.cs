using Accounting.Application.Auth.DTOs;
using Accounting.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Accounting.API.Controllers;

/// <summary>
/// Authentication: login, token refresh, logout.
/// [AllowAnonymous] — no authentication required on any action here.
/// Rate limited: 10 requests per IP per minute to prevent brute-force attacks.
/// </summary>
[AllowAnonymous]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    // ── POST /api/auth/login ──────────────────────────────────────────────────

    /// <summary>
    /// Exchange username + password for a JWT access token and a refresh token.
    /// Returns 401 on invalid credentials or inactive account.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip       = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _authService.LoginAsync(request, ip, ct);
        return Ok(response);
    }

    // ── POST /api/auth/refresh ────────────────────────────────────────────────

    /// <summary>
    /// Exchange a valid refresh token for a new access token + rotated refresh token.
    /// The old refresh token is revoked immediately (rotation strategy).
    /// Returns 401 if token is expired, revoked, or not found.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var ip       = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _authService.RefreshAsync(request.RefreshToken, ip, ct);
        return Ok(response);
    }

    // ── POST /api/auth/logout ─────────────────────────────────────────────────

    /// <summary>
    /// Revokes the given refresh token (logout from one device).
    /// Returns 204 on success. Returns 401 if token is invalid or already revoked.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await _authService.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }
}

