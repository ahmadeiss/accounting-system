using Accounting.Application.Auth;
using Accounting.Application.Sales.DTOs;
using Accounting.Application.Sales.Services;
using Accounting.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.API.Controllers;

/// <summary>
/// Sales invoice lifecycle: create (Draft) → confirm (FEFO stock deducted, Completed).
/// </summary>
public class SalesInvoicesController : BaseController
{
    private readonly ISalesInvoiceService _service;
    private readonly ICurrentUserService _currentUser;

    public SalesInvoicesController(ISalesInvoiceService service, ICurrentUserService currentUser)
    {
        _service     = service;
        _currentUser = currentUser;
    }

    // ─── GET /api/v1/sales-invoices/{id} ─────────────────────────────────────

    /// <summary>
    /// Returns a sales invoice with all lines and their batch allocations.
    /// Allocations are populated only after confirmation.
    /// </summary>
    [HttpGet("{id:guid}", Name = "GetSalesInvoiceById")]
    [ProducesResponseType(typeof(SalesInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    // ─── POST /api/v1/sales-invoices ─────────────────────────────────────────

    /// <summary>Creates a new sales invoice in Draft status. Stock is NOT touched at this stage.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionNames.SalesRead)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSalesInvoiceRequest request,
        CancellationToken ct)
    {
        var id = await _service.CreateAsync(request, _currentUser.UserId, ct);

        return CreatedAtRoute(
            "GetSalesInvoiceById",
            new { id },
            new { id });
    }

    // ─── POST /api/v1/sales-invoices/{id}/confirm ────────────────────────────

    /// <summary>
    /// Confirms a Draft sales invoice.
    ///
    /// What happens:
    ///   - FEFO batch allocation for each batch-tracked line.
    ///   - Negative StockMovement per batch consumed.
    ///   - SalesInvoiceLineAllocation records created for full traceability.
    ///   - Invoice status → Completed.
    ///   - Audit log written.
    ///   - All in one atomic transaction.
    ///
    /// Returns 409 Conflict if invoice is not in Draft status.
    /// Returns 422 Unprocessable Entity if stock is insufficient or a batch is expired.
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = PermissionNames.SalesConfirm)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        await _service.ConfirmAsync(id, _currentUser.UserId, ct);
        return NoContent();
    }
}

