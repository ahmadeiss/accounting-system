using Accounting.Application.Auth.DTOs;
using Accounting.Core.Entities;
using Accounting.Core.Exceptions;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Accounting.Application.Tests.Auth;

/// <summary>
/// Integration-style tests for AuthService using SQLite in-memory.
///
/// Scenarios:
///   1. Valid credentials → returns access token + refresh token.
///   2. Wrong password → throws UnauthorizedException.
///   3. Unknown username → throws UnauthorizedException.
///   4. Inactive account → throws UnauthorizedException.
///   5. Refresh with active token → rotates token, returns new pair.
///   6. Refresh with expired/revoked token → throws UnauthorizedException.
///   7. Revoke → token becomes inactive; subsequent refresh throws.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AccountingDbContext _db;
    private readonly AuthService _authService;

    // Shared IDs
    private static readonly Guid _roleId = Guid.NewGuid();
    private static readonly Guid _permId  = Guid.NewGuid();

    public AuthServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AccountingDbContext(options);
        _db.Database.EnsureCreated();

        SeedDatabase();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:JwtSecret"]          = "test-secret-key-at-least-32-chars-long!!",
                ["Auth:JwtIssuer"]          = "test-issuer",
                ["Auth:JwtAudience"]        = "test-audience",
                ["Auth:AccessTokenMinutes"] = "60",
            })
            .Build();

        _authService = new AuthService(_db, config);
    }

    private void SeedDatabase()
    {
        var perm = new Permission
        {
            Id = _permId, Name = "items.read", Module = "items", Action = "read",
            Description = "Read items", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var role = new Role
        {
            Id = _roleId, Name = "Manager", Description = "Manager role",
            IsSystemRole = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var rolePermission = new RolePermission { RoleId = _roleId, PermissionId = _permId };

        _db.Permissions.Add(perm);
        _db.Roles.Add(role);
        _db.SaveChanges();
        _db.RolePermissions.Add(rolePermission);
        _db.SaveChanges();
    }

    private User CreateUser(string username = "testuser", bool isActive = true)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Password@1", workFactor: 4);
        var user = new User
        {
            Id           = Guid.NewGuid(),
            Username     = username,
            Email        = $"{username}@test.com",
            FirstName    = "Test",
            LastName     = "User",
            PasswordHash = hash,
            IsActive     = isActive,
            RoleId       = _roleId,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    // ── Scenario 1: Valid login ───────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenPair()
    {
        CreateUser();

        var response = await _authService.LoginAsync(
            new LoginRequest("testuser", "Password@1"), ipAddress: null);

        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.User.Username.Should().Be("testuser");
        response.User.Permissions.Should().Contain("items.read");
    }

    // ── Scenario 2: Wrong password ────────────────────────────────────────────

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        CreateUser("user2");

        var act = () => _authService.LoginAsync(new LoginRequest("user2", "WrongPass"), null);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid*");
    }

    // ── Scenario 3: Unknown username ──────────────────────────────────────────

    [Fact]
    public async Task Login_UnknownUsername_ThrowsUnauthorized()
    {
        var act = () => _authService.LoginAsync(new LoginRequest("nobody", "Password@1"), null);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Scenario 4: Inactive account ──────────────────────────────────────────

    [Fact]
    public async Task Login_InactiveUser_ThrowsUnauthorized()
    {
        CreateUser("inactive", isActive: false);

        var act = () => _authService.LoginAsync(new LoginRequest("inactive", "Password@1"), null);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*inactive*");
    }

    // ── Scenario 5: Refresh with active token ─────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndReturnsNewPair()
    {
        CreateUser("user5");

        var first = await _authService.LoginAsync(new LoginRequest("user5", "Password@1"), null);
        var second = await _authService.RefreshAsync(first.RefreshToken, null);

        second.AccessToken.Should().NotBeNullOrEmpty();
        second.RefreshToken.Should().NotBe(first.RefreshToken);

        // Old token must be revoked
        var old = await _db.RefreshTokens.SingleAsync(t => t.Token == first.RefreshToken);
        old.RevokedAt.Should().NotBeNull();
    }

    // ── Scenario 6: Refresh with expired/revoked token ────────────────────────

    [Fact]
    public async Task Refresh_RevokedToken_ThrowsUnauthorized()
    {
        CreateUser("user6");

        var first = await _authService.LoginAsync(new LoginRequest("user6", "Password@1"), null);
        // Rotate once — first token is now revoked
        await _authService.RefreshAsync(first.RefreshToken, null);

        // Try to reuse the revoked token
        var act = () => _authService.RefreshAsync(first.RefreshToken, null);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── Scenario 7: Revoke → subsequent refresh throws ────────────────────────

    [Fact]
    public async Task Revoke_ThenRefresh_ThrowsUnauthorized()
    {
        CreateUser("user7");

        var login = await _authService.LoginAsync(new LoginRequest("user7", "Password@1"), null);
        await _authService.RevokeAsync(login.RefreshToken);

        var act = () => _authService.RefreshAsync(login.RefreshToken, null);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

