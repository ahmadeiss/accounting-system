using System.Text.Json;
using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Background scanning engine. Registered as a Hangfire recurring job (every hour).
///
/// Each ScanAsync pass:
///   1. Scans StockBalances for LowStock conditions.
///   2. Scans ItemBatches for NearExpiry conditions.
///   3. Scans ItemBatches for Expired conditions.
///   4. Calls AutoResolveStaleAsync for each alert type to retire alerts whose conditions cleared.
///   5. Commits all changes in a single SaveChangesAsync call.
///
/// Idempotent: running the scanner multiple times never creates duplicate Active alerts.
/// </summary>
public class AlertScanner
{
    private readonly AccountingDbContext _context;
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertScanner> _logger;
    private readonly int _nearExpiryDays;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AlertScanner(
        AccountingDbContext context,
        IAlertService alertService,
        IConfiguration configuration,
        ILogger<AlertScanner> logger)
    {
        _context = context;
        _alertService = alertService;
        _logger = logger;
        _nearExpiryDays = int.TryParse(configuration["Alerts:NearExpiryDays"], out var d) ? d : 30;
    }

    // ─── Entry point (Hangfire calls this) ────────────────────────────────────

    public async Task ScanAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("AlertScanner: starting scan pass (NearExpiryDays={Days})", _nearExpiryDays);

        var lowStockActive = await ScanLowStockAsync(ct);
        var nearExpiryActive = await ScanNearExpiryAsync(ct);
        var expiredActive = await ScanExpiredAsync(ct);

        // Auto-resolve stale alerts for each type
        await _alertService.AutoResolveStaleAsync(AlertType.LowStock, lowStockActive, ct);
        await _alertService.AutoResolveStaleAsync(AlertType.NearExpiry, nearExpiryActive, ct);
        await _alertService.AutoResolveStaleAsync(AlertType.ExpiredStock, expiredActive, ct);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "AlertScanner: scan complete. LowStock={L}, NearExpiry={N}, Expired={E}",
            lowStockActive.Count, nearExpiryActive.Count, expiredActive.Count);
    }

    // ─── Low Stock ────────────────────────────────────────────────────────────

    private async Task<List<Guid>> ScanLowStockAsync(CancellationToken ct)
    {
        // Fetch balances where qty <= reorder level, for active items that have a reorder level > 0.
        var balances = await _context.StockBalances
            .Include(b => b.Item)
            .Include(b => b.Warehouse)
            .Where(b => b.Item.IsActive
                     && b.Item.ReorderLevel > 0
                     && b.QuantityOnHand <= b.Item.ReorderLevel)
            .ToListAsync(ct);

        var activeIds = new List<Guid>();

        foreach (var balance in balances)
        {
            var severity = balance.QuantityOnHand <= 0
                ? AlertSeverity.Critical
                : AlertSeverity.Warning;

            var message = balance.QuantityOnHand <= 0
                ? $"'{balance.Item.Name}' is out of stock in warehouse '{balance.Warehouse.Name}'."
                : $"'{balance.Item.Name}' stock ({balance.QuantityOnHand:G}) is at or below reorder level ({balance.Item.ReorderLevel:G}) in '{balance.Warehouse.Name}'.";

            var metadata = JsonSerializer.Serialize(new
            {
                quantityOnHand = balance.QuantityOnHand,
                reorderLevel = balance.Item.ReorderLevel,
                warehouseName = balance.Warehouse.Name
            }, _json);

            var alert = await _alertService.CreateOrUpdateAlertAsync(
                AlertType.LowStock,
                severity,
                message,
                itemId: balance.ItemId,
                itemBatchId: null,
                warehouseId: balance.WarehouseId,
                metadata: metadata,
                ct: ct);

            activeIds.Add(alert.Id);
        }

        return activeIds;
    }

    // ─── Near Expiry ──────────────────────────────────────────────────────────

    private async Task<List<Guid>> ScanNearExpiryAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(_nearExpiryDays);

        // Batches that expire within the threshold window and still have stock.
        var batches = await _context.ItemBatches
            .Include(b => b.Item)
            .Include(b => b.Warehouse)
            .Where(b => b.Item.TrackExpiry
                     && b.ExpiryDate.HasValue
                     && b.ExpiryDate.Value >= today          // not yet expired
                     && b.ExpiryDate.Value <= threshold       // expires within window
                     && b.AvailableQuantity > 0
                     && b.Status == BatchStatus.Active)
            .ToListAsync(ct);

        var activeIds = new List<Guid>();

        foreach (var batch in batches)
        {
            var daysLeft = batch.ExpiryDate!.Value.DayNumber - today.DayNumber;

            var severity = daysLeft <= 7 ? AlertSeverity.Critical
                         : daysLeft <= 14 ? AlertSeverity.Warning
                         : AlertSeverity.Info;

            var message = $"Batch '{batch.BatchNumber}' of '{batch.Item.Name}' expires on {batch.ExpiryDate:yyyy-MM-dd} ({daysLeft} days). Qty: {batch.AvailableQuantity:G}.";

            var metadata = JsonSerializer.Serialize(new
            {
                batchNumber = batch.BatchNumber,
                expiryDate = batch.ExpiryDate!.Value.ToString("yyyy-MM-dd"),
                daysUntilExpiry = daysLeft,
                availableQuantity = batch.AvailableQuantity
            }, _json);

            var alert = await _alertService.CreateOrUpdateAlertAsync(
                AlertType.NearExpiry,
                severity,
                message,
                itemId: batch.ItemId,
                itemBatchId: batch.Id,
                warehouseId: batch.WarehouseId,
                metadata: metadata,
                ct: ct);

            activeIds.Add(alert.Id);
        }

        return activeIds;
    }

    // ─── Expired ──────────────────────────────────────────────────────────────

    private async Task<List<Guid>> ScanExpiredAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var batches = await _context.ItemBatches
            .Include(b => b.Item)
            .Include(b => b.Warehouse)
            .Where(b => b.Item.TrackExpiry
                     && b.ExpiryDate.HasValue
                     && b.ExpiryDate.Value < today
                     && b.AvailableQuantity > 0)
            .ToListAsync(ct);

        var activeIds = new List<Guid>();

        foreach (var batch in batches)
        {
            var message = $"Batch '{batch.BatchNumber}' of '{batch.Item.Name}' expired on {batch.ExpiryDate:yyyy-MM-dd} with {batch.AvailableQuantity:G} units still in stock. Immediate action required.";

            var metadata = JsonSerializer.Serialize(new
            {
                batchNumber = batch.BatchNumber,
                expiryDate = batch.ExpiryDate!.Value.ToString("yyyy-MM-dd"),
                availableQuantity = batch.AvailableQuantity
            }, _json);

            var alert = await _alertService.CreateOrUpdateAlertAsync(
                AlertType.ExpiredStock,
                AlertSeverity.Critical,
                message,
                itemId: batch.ItemId,
                itemBatchId: batch.Id,
                warehouseId: batch.WarehouseId,
                metadata: metadata,
                ct: ct);

            activeIds.Add(alert.Id);
        }

        return activeIds;
    }
}

