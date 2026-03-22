using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Accounting.Application.Auth.DTOs;
using Accounting.Application.Auth.Interfaces;
using Accounting.Core.Entities;
using Accounting.Core.Exceptions;
using Accounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Handles credential validation, JWT issuance, and refresh token rotation.
///
/// Security decisions:
///   - BCrypt work factor 12 (≈250ms on modern hardware — acceptable for login, not for hot paths).
///   - JWT signed with HMAC-SHA256. Secret must be ≥32 chars; enforced at startup.
///   - Refresh tokens are 64-byte CSPRNG values, base64url-encoded, stored in plaintext (MVP).
///   - Rotation: each use of a refresh token revokes the old one and issues a new one.
///   - Expired/revoked tokens are kept for 30 days for audit, then purged (future scheduled job).
/// </summary>
public class AuthService : IAuthService
{
    private readonly AccountingDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AccountingDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid username or password.", "INVALID_CREDENTIALS");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is inactive. Contact your administrator.", "ACCOUNT_INACTIVE");

        user.LastLoginAt = DateTime.UtcNow;

        var refreshToken = CreateRefreshToken(user.Id, ipAddress);
        _db.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync(ct);

        return BuildResponse(user, refreshToken.Token);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    public async Task<LoginResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct);

        if (token is null || !token.IsActive)
            throw new UnauthorizedException("Refresh token is invalid, expired, or revoked.", "INVALID_REFRESH_TOKEN");

        if (!token.User.IsActive)
            throw new UnauthorizedException("Account is inactive.", "ACCOUNT_INACTIVE");

        // Rotate: revoke old, issue new
        var newToken = CreateRefreshToken(token.UserId, ipAddress);
        token.RevokedAt        = DateTime.UtcNow;
        token.ReplacedByToken  = newToken.Token;

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(ct);

        return BuildResponse(token.User, newToken.Token);
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct);

        if (token is null || !token.IsActive)
            throw new UnauthorizedException("Refresh token is invalid or already revoked.", "INVALID_REFRESH_TOKEN");

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private LoginResponse BuildResponse(User user, string refreshTokenValue)
    {
        var permissions = user.Role.RolePermissions
            .Select(rp => rp.Permission.Name)
            .OrderBy(n => n)
            .ToList();

        var (accessToken, expiresAt) = GenerateJwt(user, permissions);

        var profile = new UserProfileDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role.Name,
            user.BranchId,
            permissions);

        return new LoginResponse(accessToken, refreshTokenValue, expiresAt, profile);
    }

    private (string token, DateTime expiresAt) GenerateJwt(User user, IReadOnlyList<string> permissions)
    {
        var secret  = _config["Auth:JwtSecret"]
            ?? throw new InvalidOperationException("Auth:JwtSecret is not configured.");
        var issuer   = _config["Auth:JwtIssuer"]   ?? "accounting-api";
        var audience = _config["Auth:JwtAudience"] ?? "accounting-client";
        var minutes  = int.Parse(_config["Auth:AccessTokenMinutes"] ?? "60");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role",     user.Role.Name),
            new("fullName", user.FullName),
        };

        if (user.BranchId.HasValue)
            claims.Add(new Claim("branchId", user.BranchId.Value.ToString()));

        // Embed permissions as individual claims so ASP.NET Core policy checks work without a DB hit
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
        var jwt = new JwtSecurityToken(issuer, audience, claims,
            expires: expiresAt, signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }

    private static RefreshToken CreateRefreshToken(Guid userId, string? ipAddress)
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return new RefreshToken
        {
            Id           = Guid.NewGuid(),
            UserId       = userId,
            Token        = Convert.ToBase64String(bytes),
            ExpiresAt    = DateTime.UtcNow.AddDays(30),
            CreatedByIp  = ipAddress,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };
    }
}

