using Accounting.Application.Auth.DTOs;

namespace Accounting.Application.Auth.Interfaces;

public interface IRoleService
{
    Task<Guid> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RoleListDto>> ListAsync(CancellationToken ct = default);
    Task AssignPermissionsAsync(Guid roleId, IReadOnlyList<string> permissionNames, CancellationToken ct = default);
}

