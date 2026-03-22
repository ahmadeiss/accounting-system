using Accounting.Application.Auth.DTOs;

namespace Accounting.Application.Auth.Interfaces;

/// <summary>
/// Handles credential validation, JWT issuance, and refresh token lifecycle.
/// All security logic lives here — controllers are pure routing.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates credentials and returns a JWT access token + refresh token pair.
    /// Throws <see cref="Accounting.Core.Exceptions.UnauthorizedException"/> on bad credentials or inactive account.
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Validates an existing refresh token, revokes it, and issues a new token pair (rotation).
    /// Throws <see cref="Accounting.Core.Exceptions.UnauthorizedException"/> if token is invalid, expired, or revoked.
    /// </summary>
    Task<LoginResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Revokes a specific refresh token (logout from one device).
    /// Throws <see cref="Accounting.Core.Exceptions.UnauthorizedException"/> if token not found or already revoked.
    /// </summary>
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}

