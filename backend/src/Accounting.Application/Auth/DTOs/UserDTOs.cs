namespace Accounting.Application.Auth.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public record CreateUserRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    Guid RoleId,
    Guid? BranchId);

public record AssignRoleRequest(Guid RoleId);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string RoleName,
    bool IsActive,
    Guid? BranchId,
    IReadOnlyList<string> Permissions,
    DateTime CreatedAt);

public record UserListDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string RoleName,
    bool IsActive,
    DateTime CreatedAt);

