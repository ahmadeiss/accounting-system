using Accounting.Application.Auth;
using Accounting.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.API.Controllers;

/// <summary>
/// Warehouse lookup — used to populate the warehouse dropdown in the purchase invoice
/// and opening stock import forms.  Returns branch info so the form can auto-derive BranchId.
/// </summary>
public class WarehousesController : BaseController
{
    private readonly AccountingDbContext _context;

    public WarehousesController(AccountingDbContext context) => _context = context;

    /// <summary>Returns all active warehouses with their branch info, ordered by name.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionNames.PurchasesRead)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var warehouses = await _context.Warehouses
            .AsNoTracking()
            .Include(w => w.Branch)
            .Where(w => w.IsActive)
            .OrderBy(w => w.Name)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.Code,
                w.IsDefault,
                BranchId   = w.BranchId,
                BranchName = w.Branch.Name,
            })
            .ToListAsync(ct);

        return Ok(warehouses);
    }
}

