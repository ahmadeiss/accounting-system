using Accounting.Application.Auth;
using Accounting.Application.Auth.DTOs;
using Accounting.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.API.Controllers;

/// <summary>
/// Role management. All actions require roles.manage permission.
/// </summary>
public class RolesController : BaseController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService) => _roleService = roleService;

    // ── GET /api/v1/roles ─────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = PermissionNames.RolesManage)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await _roleService.ListAsync(ct));

    // ── POST /api/v1/roles ────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = PermissionNames.RolesManage)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var id = await _roleService.CreateAsync(request, ct);
        return CreatedAtRoute(null, new { id }, new { id });
    }

    // ── PUT /api/v1/roles/{id}/permissions ────────────────────────────────────

    /// <summary>
    /// Replaces the full permission set for a role.
    /// Not allowed on system roles (Admin) — returns 422.
    /// Send an empty list to revoke all permissions.
    /// </summary>
    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = PermissionNames.RolesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssignPermissions(
        Guid id,
        [FromBody] AssignPermissionsRequest request,
        CancellationToken ct)
    {
        await _roleService.AssignPermissionsAsync(id, request.PermissionNames, ct);
        return NoContent();
    }
}

