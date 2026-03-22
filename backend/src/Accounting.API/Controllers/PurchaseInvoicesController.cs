using Accounting.Application.Auth;
using Accounting.Application.Purchasing.DTOs;
using Accounting.Application.Purchasing.Services;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting.API.Controllers;

/// <summary>
/// Purchase invoice lifecycle: create (Draft) → confirm (stock received).
/// </summary>
public class PurchaseInvoicesController : BaseController
{
    private readonly IPurchaseInvoiceService _service;
    private readonly ICurrentUserService     _currentUser;
    private readonly AccountingDbContext     _context;

    public PurchaseInvoicesController(
        IPurchaseInvoiceService service,
        ICurrentUserService currentUser,
        AccountingDbContext context)
    {
        _service     = service;
        _currentUser = currentUser;
        _context     = context;
    }

    // ─── GET /api/v1/purchase-invoices ───────────────────────────────────────

    /// <summary>Returns a paginated list of purchase invoices with optional filters.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionNames.PurchasesRead)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int    page        = 1,
        [FromQuery] int    pageSize    = 20,
        [FromQuery] string? status     = null,
        [FromQuery] Guid?  supplierId  = null,
        [FromQuery] Guid?  warehouseId = null,
        CancellationToken ct = default)
    {
        var query = _context.PurchaseInvoices
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status.ToString() == status);

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        if (warehouseId.HasValue)
            query = query.Where(p => p.WarehouseId == warehouseId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseInvoiceListDto(
                p.Id,
                p.InvoiceNumber,
                p.Supplier.Name,
                p.Warehouse.Name,
                p.InvoiceDate,
                p.DueDate,
                p.Status.ToString(),
                p.Lines.Count,
                p.TotalAmount,
                p.CreatedAt))
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    // ─── GET /api/v1/purchase-invoices/{id} ──────────────────────────────────

    /// <summary>Returns a purchase invoice with all lines.</summary>
    [HttpGet("{id:guid}", Name = "GetPurchaseInvoiceById")]
    [Authorize(Policy = PermissionNames.PurchasesRead)]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    // ─── POST /api/v1/purchase-invoices ──────────────────────────────────────

    /// <summary>Creates a new purchase invoice in Draft status.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionNames.PurchasesRead)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePurchaseInvoiceRequest request,
        CancellationToken ct)
    {
        var id = await _service.CreateAsync(request, _currentUser.UserId, ct);
        return CreatedAtRoute("GetPurchaseInvoiceById", new { id }, new { id });
    }

    // ─── POST /api/v1/purchase-invoices/{id}/confirm ─────────────────────────

    /// <summary>Confirms a Draft invoice — creates batches, records stock, writes audit.</summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = PermissionNames.PurchasesConfirm)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        await _service.ConfirmAsync(id, _currentUser.UserId, ct);
        return NoContent();
    }
}

