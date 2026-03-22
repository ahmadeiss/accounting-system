using Accounting.Application.Auth.DTOs;
using Accounting.Application.Auth.Interfaces;
using Accounting.Core.Entities;
using Accounting.Core.Exceptions;
using Accounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly AccountingDbContext _db;

    public RoleService(AccountingDbContext db) => _db = db;

    public async Task<Guid> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == request.Name, ct))
            throw new DuplicateEntityException("Role", "Name", request.Name);

        var role = new Role
        {
            Id           = Guid.NewGuid(),
            Name         = request.Name,
            Description  = request.Description,
            IsSystemRole = false,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role.Id;
    }

    public async Task<IReadOnlyList<RoleListDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleListDto(r.Id, r.Name, r.Description, r.IsSystemRole))
            .ToListAsync(ct);
    }

    public async Task AssignPermissionsAsync(Guid roleId, IReadOnlyList<string> permissionNames, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new NotFoundException("Role", roleId);

        if (role.IsSystemRole)
            throw new DomainException("System roles cannot have their permissions modified via the API.", "SYSTEM_ROLE_IMMUTABLE");

        // Validate all permission names exist
        var permissions = await _db.Permissions
            .Where(p => permissionNames.Contains(p.Name))
            .ToListAsync(ct);

        var unknown = permissionNames.Except(permissions.Select(p => p.Name)).ToList();
        if (unknown.Any())
            throw new DomainException($"Unknown permissions: {string.Join(", ", unknown)}", "UNKNOWN_PERMISSIONS");

        // Replace all permissions (full replace, not additive)
        _db.RolePermissions.RemoveRange(role.RolePermissions);

        foreach (var perm in permissions)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId       = roleId,
                PermissionId = perm.Id,
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}

