using Accounting.Application.Alerts.DTOs;
using Accounting.Application.Auth;
using Accounting.Core.Enums;
using Accounting.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.API.Controllers;

/// <summary>
/// Operational alerts API.
///
/// Alert logic lives entirely in IAlertService and AlertScanner.
/// This controller only does routing, mapping, and HTTP status translation.
///
/// Auth is deferred — user identity will come from JWT claim.
/// </summary>
[Route("api/v1/alerts")]
public class AlertsController : BaseController
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    // ─── GET /api/v1/alerts ───────────────────────────────────────────────────

    /// <summary>
    /// Returns alerts with optional filters.
    /// All three query parameters are optional and can be combined.
    /// </summary>
    /// <param name="type">LowStock | NearExpiry | ExpiredStock | BatchRecalled</param>
    /// <param name="status">Active | Acknowledged | Resolved</param>
    /// <param name="severity">Info | Warning | Critical</param>
    [HttpGet]
    [Authorize(Policy = PermissionNames.AlertsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] AlertType? type = null,
        [FromQuery] AlertStatus? status = null,
        [FromQuery] AlertSeverity? severity = null,
        CancellationToken ct = default)
    {
        var alerts = await _alertService.GetAlertsAsync(type, status, severity, ct);

        var dtos = alerts.Select(a => new AlertDto(
            Id: a.Id,
            AlertType: a.AlertType.ToString(),
            Severity: a.Severity.ToString(),
            Status: a.Status.ToString(),
            Message: a.Message,
            ItemId: a.ItemId,
            ItemName: a.Item?.Name,
            ItemSku: a.Item?.SKU,
            ItemBatchId: a.ItemBatchId,
            BatchNumber: a.ItemBatch?.BatchNumber,
            ExpiryDate: a.ItemBatch?.ExpiryDate,
            WarehouseId: a.WarehouseId,
            WarehouseName: a.Warehouse?.Name,
            Metadata: a.Metadata,
            CreatedAt: a.CreatedAt,
            UpdatedAt: a.UpdatedAt
        )).ToList();

        return Ok(dtos);
    }

    // ─── POST /api/v1/alerts/{id}/acknowledge ─────────────────────────────────

    /// <summary>
    /// Marks an alert as Acknowledged (seen, but not yet resolved).
    /// Transitions: Active → Acknowledged.
    /// Cannot acknowledge an already-Resolved alert.
    /// </summary>
    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Policy = PermissionNames.AlertsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
    {
        try
        {
            await _alertService.AcknowledgeAsync(id, ct);
            await SaveChangesAsync(ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // ─── POST /api/v1/alerts/{id}/resolve ────────────────────────────────────

    /// <summary>
    /// Marks an alert as Resolved.
    /// Transitions: Active or Acknowledged → Resolved.
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = PermissionNames.AlertsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        try
        {
            await _alertService.ResolveAsync(id, ct);
            await SaveChangesAsync(ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    // Injecting the DbContext directly for SaveChanges would break layering.
    // We use the IUnitOfWork already registered in the container.
    private Task SaveChangesAsync(CancellationToken ct)
    {
        // AlertService works directly on AccountingDbContext (same pattern as AuditService).
        // Resolve UoW from HttpContext to commit.
        var uow = HttpContext.RequestServices.GetRequiredService<Accounting.Core.Interfaces.IUnitOfWork>();
        return uow.SaveChangesAsync(ct);
    }
}

