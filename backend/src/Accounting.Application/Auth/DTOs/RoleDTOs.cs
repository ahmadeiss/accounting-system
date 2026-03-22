namespace Accounting.Application.Auth.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public record CreateRoleRequest(string Name, string Description);

public record AssignPermissionsRequest(IReadOnlyList<string> PermissionNames);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    IReadOnlyList<string> Permissions);

public record RoleListDto(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole);

