using Accounting.Application.Auth;
using Accounting.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.API.Controllers;

/// <summary>
/// Unit-of-measure lookup — used to populate dropdowns in item create/edit forms.
/// </summary>
public class UnitsController : BaseController
{
    private readonly AccountingDbContext _context;

    public UnitsController(AccountingDbContext context) => _context = context;

    /// <summary>Returns all active units ordered by name.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionNames.ItemsRead)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var units = await _context.Units
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, u.Abbreviation })
            .ToListAsync(ct);

        return Ok(units);
    }
}

