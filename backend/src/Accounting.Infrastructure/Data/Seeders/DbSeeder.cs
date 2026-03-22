using Accounting.Application.Auth;
using Accounting.Core.Entities;
using Accounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Data.Seeders;

/// <summary>
/// Idempotent seeder. Safe to run on every startup.
/// Seeds: all known permissions, the Admin system role, and the initial admin user.
///
/// Admin credentials are read from configuration:
///   Auth:AdminUsername  (default: admin)
///   Auth:AdminEmail     (default: admin@system.local)
///   Auth:AdminPassword  (REQUIRED — no default; startup fails if missing)
/// </summary>
public class DbSeeder
{
    private readonly AccountingDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(AccountingDbContext db, IConfiguration config, ILogger<DbSeeder> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        var adminRoleId = await SeedAdminRoleAsync(ct);
        await SeedAdminUserAsync(adminRoleId, ct);
    }

    // ── Permissions ───────────────────────────────────────────────────────────

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var existing = await _db.Permissions.Select(p => p.Name).ToListAsync(ct);
        var missing  = PermissionNames.All.Except(existing).ToList();

        if (!missing.Any()) return;

        foreach (var name in missing)
        {
            var parts = name.Split('.', 2);
            _db.Permissions.Add(new Permission
            {
                Id          = Guid.NewGuid(),
                Name        = name,
                Module      = parts[0],
                Action      = parts.Length > 1 ? parts[1] : name,
                Description = name,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} new permissions.", missing.Count);
    }

    // ── Admin role ────────────────────────────────────────────────────────────

    private async Task<Guid> SeedAdminRoleAsync(CancellationToken ct)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == "Admin", ct);

        if (role is null)
        {
            role = new Role
            {
                Id           = Guid.NewGuid(),
                Name         = "Admin",
                Description  = "Full system access",
                IsSystemRole = true,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow,
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded Admin role.");
        }

        // Ensure Admin has ALL permissions
        var allPermissions = await _db.Permissions.ToListAsync(ct);
        var assignedIds    = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        foreach (var perm in allPermissions.Where(p => !assignedIds.Contains(p.Id)))
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId       = role.Id,
                PermissionId = perm.Id,
            });
        }

        await _db.SaveChangesAsync(ct);
        return role.Id;
    }

    // ── Admin user ────────────────────────────────────────────────────────────

    private async Task SeedAdminUserAsync(Guid adminRoleId, CancellationToken ct)
    {
        var username = _config["Auth:AdminUsername"] ?? "admin";
        var email    = _config["Auth:AdminEmail"]    ?? "admin@system.local";
        var password = _config["Auth:AdminPassword"]
            ?? throw new InvalidOperationException(
                "Auth:AdminPassword must be set in configuration before first startup.");

        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            return;

        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        _db.Users.Add(new User
        {
            Id           = Guid.NewGuid(),
            Username     = username,
            Email        = email,
            FirstName    = "System",
            LastName     = "Admin",
            PasswordHash = hash,
            IsActive     = true,
            RoleId       = adminRoleId,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded admin user '{Username}'.", username);
    }
}

