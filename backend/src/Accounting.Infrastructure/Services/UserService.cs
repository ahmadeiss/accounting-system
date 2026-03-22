using Accounting.Application.Auth.DTOs;
using Accounting.Application.Auth.Interfaces;
using Accounting.Core.Entities;
using Accounting.Core.Exceptions;
using Accounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AccountingDbContext _db;

    public UserService(AccountingDbContext db) => _db = db;

    public async Task<Guid> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username, ct))
            throw new DuplicateEntityException("User", "Username", request.Username);

        if (await _db.Users.AnyAsync(u => u.Email == request.Email, ct))
            throw new DuplicateEntityException("User", "Email", request.Email);

        if (!await _db.Roles.AnyAsync(r => r.Id == request.RoleId, ct))
            throw new NotFoundException("Role", request.RoleId);

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        var user = new User
        {
            Id           = Guid.NewGuid(),
            Username     = request.Username,
            Email        = request.Email,
            FirstName    = request.FirstName,
            LastName     = request.LastName,
            PasswordHash = hash,
            IsActive     = true,
            RoleId       = request.RoleId,
            BranchId     = request.BranchId,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User", id);

        return ToDto(user);
    }

    public async Task<IReadOnlyList<UserListDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Username)
            .Select(u => new UserListDto(
                u.Id, u.Username, u.Email, u.FullName,
                u.Role.Name, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct)
            ?? throw new NotFoundException("User", id);
        user.IsActive  = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct)
            ?? throw new NotFoundException("User", id);
        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("User", userId);

        if (!await _db.Roles.AnyAsync(r => r.Id == roleId, ct))
            throw new NotFoundException("Role", roleId);

        user.RoleId    = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.Username, u.Email, u.FullName,
        u.Role.Name, u.IsActive, u.BranchId,
        u.Role.RolePermissions.Select(rp => rp.Permission.Name).OrderBy(n => n).ToList(),
        u.CreatedAt);
}

