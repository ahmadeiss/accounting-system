namespace Accounting.Core.Entities;

/// <summary>
/// A revocable, rotatable refresh token for JWT auth.
///
/// Security model (MVP):
///   - Token value is a 64-byte cryptographically random string, base64-encoded.
///   - Stored in plaintext (acceptable for MVP; upgrade to SHA-256 hash + constant-time compare for hardened environments).
///   - One token per login session. Rotation creates a new token and revokes the old one.
///   - Expired and revoked tokens are retained for 30 days for audit purposes, then purged.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Opaque secure random token value (base64url, 64 bytes).</summary>
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    /// <summary>Token that replaced this one during rotation (for rotation chain tracing).</summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>IP address of the client that created this token. Optional, for audit.</summary>
    public string? CreatedByIp { get; set; }

    // ── Computed state ───────────────────────────────────────────────────────
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive  => !IsRevoked && !IsExpired;
}

