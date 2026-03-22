using Accounting.Application.Auth;
using Accounting.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.API.Controllers;

/// <summary>
/// Supplier lookup — used to populate the supplier dropdown in the purchase invoice form.
/// </summary>
public class SuppliersController : BaseController
{
    private readonly AccountingDbContext _context;

    public SuppliersController(AccountingDbContext context) => _context = context;

    /// <summary>Returns all active suppliers ordered by name.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionNames.PurchasesRead)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Code })
            .ToListAsync(ct);

        return Ok(suppliers);
    }
}

