namespace Accounting.Core.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user's context.
/// Implemented in the API layer using IHttpContextAccessor.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Username { get; }
    string Email { get; }
    Guid? BranchId { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permissionName);
}

