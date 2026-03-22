using System.Security.Claims;
using Accounting.Core.Interfaces;

namespace Accounting.API.Services;

/// <summary>
/// Resolves the current user's identity from the JWT claims in the HTTP context.
/// Registered as Scoped — one instance per request.
///
/// Claim layout (set by AuthService.GenerateJwt):
///   sub         → UserId (Guid)
///   unique_name → Username
///   email       → Email
///   branchId    → BranchId (optional)
///   permission  → one claim per permission (multi-value)
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal
        => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated
        => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string Username
        => Principal?.FindFirstValue(ClaimTypes.Name)
        ?? Principal?.FindFirstValue("unique_name")
        ?? string.Empty;

    public string Email
        => Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email")
        ?? string.Empty;

    public Guid? BranchId
    {
        get
        {
            var val = Principal?.FindFirstValue("branchId");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public bool HasPermission(string permissionName)
        => Principal?.Claims
               .Where(c => c.Type == "permission")
               .Any(c => c.Value == permissionName)
           == true;
}

