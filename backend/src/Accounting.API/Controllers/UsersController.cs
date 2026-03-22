using Accounting.Application.Auth;
using Accounting.Application.Auth.DTOs;
using Accounting.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.API.Controllers;

/// <summary>
/// User management. All actions require users.manage permission.
///
/// Example authorization flow:
///   1. Client logs in via POST /api/auth/login → receives JWT.
///   2. JWT contains claim  "permission": "users.manage"  for Admin users.
///   3. [Authorize(Policy = PermissionNames.UsersManage)] validates that claim.
///   4. Requests without the claim receive HTTP 403 Forbidden.
/// </summary>
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    // ── GET /api/v1/users ─────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = PermissionNames.UsersManage)]
    [ProducesResponseType(typeof(IReadOnlyList<UserListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await _userService.ListAsync(ct));

    // ── GET /api/v1/users/{id} ────────────────────────────────────────────────

    [HttpGet("{id:guid}", Name = "GetUserById")]
    [Authorize(Policy = PermissionNames.UsersManage)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _userService.GetByIdAsync(id, ct));

    // ── POST /api/v1/users ────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = PermissionNames.UsersManage)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var id = await _userService.CreateAsync(request, ct);
        return CreatedAtRoute("GetUserById", new { id }, new { id });
    }

    // ── POST /api/v1/users/{id}/assign-role ───────────────────────────────────

    [HttpPost("{id:guid}/assign-role")]
    [Authorize(Policy = PermissionNames.UsersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        await _userService.AssignRoleAsync(id, request.RoleId, ct);
        return NoContent();
    }

    // ── POST /api/v1/users/{id}/activate ─────────────────────────────────────

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionNames.UsersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _userService.ActivateAsync(id, ct);
        return NoContent();
    }

    // ── POST /api/v1/users/{id}/deactivate ───────────────────────────────────

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionNames.UsersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _userService.DeactivateAsync(id, ct);
        return NoContent();
    }
}

