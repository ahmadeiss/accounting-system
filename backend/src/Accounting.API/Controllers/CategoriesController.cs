using Accounting.Application.Auth;
using Accounting.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.API.Controllers;

/// <summary>
/// Category lookup — used to populate dropdowns in item create/edit forms.
/// </summary>
public class CategoriesController : BaseController
{
    private readonly AccountingDbContext _context;

    public CategoriesController(AccountingDbContext context) => _context = context;

    /// <summary>Returns all active categories ordered by name.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionNames.ItemsRead)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        return Ok(categories);
    }
}

