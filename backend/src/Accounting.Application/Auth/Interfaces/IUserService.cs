using Accounting.Application.Auth.DTOs;

namespace Accounting.Application.Auth.Interfaces;

public interface IUserService
{
    Task<Guid> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<UserListDto>> ListAsync(CancellationToken ct = default);
    Task ActivateAsync(Guid id, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);
}

