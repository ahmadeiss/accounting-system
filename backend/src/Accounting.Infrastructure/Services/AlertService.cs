using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Core alert lifecycle service.
///
/// Deduplication strategy:
///   Before creating an alert the service checks for an existing Active alert with the same
///   (AlertType + dedup key). If one exists it updates UpdatedAt (touch) instead of inserting.
///   This prevents alert spam while keeping the timestamp fresh.
///
/// Auto-resolution:
///   AlertScanner calls AutoResolveStaleAsync after each scan pass.
///   Any Active alert whose condition is gone (item restocked, batch sold, etc.) is Resolved.
///   Acknowledged alerts are NOT auto-resolved — a human acknowledged it and should close it.
///
/// SaveChangesAsync is NOT called here. The caller (AlertScanner or controller) owns the
/// transaction boundary, allowing multiple alerts to be batched in one commit.
/// </summary>
public class AlertService : IAlertService
{
    private readonly AccountingDbContext _context;

    public AlertService(AccountingDbContext context)
    {
        _context = context;
    }

    // ─── CreateOrUpdate ───────────────────────────────────────────────────────

    public async Task<Alert> CreateOrUpdateAlertAsync(
        AlertType type,
        AlertSeverity severity,
        string message,
        Guid? itemId,
        Guid? itemBatchId,
        Guid? warehouseId,
        string? metadata,
        CancellationToken ct = default)
    {
        var existing = await FindActiveAsync(type, itemId, itemBatchId, warehouseId, ct);

        if (existing is not null)
        {
            // Touch — update message/metadata in case values changed (qty dropped further, etc.)
            existing.Message = message;
            existing.Metadata = metadata;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.Alerts.Update(existing);
            return existing;
        }

        var alert = new Alert
        {
            AlertType = type,
            Severity = severity,
            Status = AlertStatus.Active,
            Message = message,
            Metadata = metadata,
            ItemId = itemId,
            ItemBatchId = itemBatchId,
            WarehouseId = warehouseId,
        };

        await _context.Alerts.AddAsync(alert, ct);
        return alert;
    }

    // ─── Lifecycle transitions ────────────────────────────────────────────────

    public async Task AcknowledgeAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await GetOrThrowAsync(alertId, ct);

        if (alert.Status == AlertStatus.Resolved)
            throw new InvalidOperationException($"Alert {alertId} is already Resolved and cannot be Acknowledged.");

        alert.Status = AlertStatus.Acknowledged;
        alert.UpdatedAt = DateTime.UtcNow;
        _context.Alerts.Update(alert);
    }

    public async Task ResolveAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await GetOrThrowAsync(alertId, ct);
        alert.Status = AlertStatus.Resolved;
        alert.UpdatedAt = DateTime.UtcNow;
        _context.Alerts.Update(alert);
    }

    // ─── Query ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Alert>> GetAlertsAsync(
        AlertType? type = null,
        AlertStatus? status = null,
        AlertSeverity? severity = null,
        CancellationToken ct = default)
    {
        var query = _context.Alerts
            .Include(a => a.Item)
            .Include(a => a.ItemBatch)
            .Include(a => a.Warehouse)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(a => a.AlertType == type.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    // ─── Auto-resolve ─────────────────────────────────────────────────────────

    public async Task AutoResolveStaleAsync(
        AlertType type,
        IEnumerable<Guid> stillActiveIds,
        CancellationToken ct = default)
    {
        // Only auto-resolve Active alerts — Acknowledged ones need human confirmation.
        var stale = await _context.Alerts
            .Where(a => a.AlertType == type
                     && a.Status == AlertStatus.Active
                     && !stillActiveIds.Contains(a.Id))
            .ToListAsync(ct);

        foreach (var a in stale)
        {
            a.Status = AlertStatus.Resolved;
            a.UpdatedAt = DateTime.UtcNow;
        }

        if (stale.Count > 0)
            _context.Alerts.UpdateRange(stale);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private Task<Alert?> FindActiveAsync(
        AlertType type,
        Guid? itemId,
        Guid? itemBatchId,
        Guid? warehouseId,
        CancellationToken ct)
    {
        return _context.Alerts.FirstOrDefaultAsync(a =>
            a.AlertType == type
            && a.Status == AlertStatus.Active
            && a.ItemId == itemId
            && a.ItemBatchId == itemBatchId
            && a.WarehouseId == warehouseId, ct);
    }

    private async Task<Alert> GetOrThrowAsync(Guid id, CancellationToken ct)
    {
        return await _context.Alerts.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Alert {id} not found.");
    }
}

