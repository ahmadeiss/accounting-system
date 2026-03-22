using Accounting.Application.Dashboard.DTOs;
using Accounting.Application.Dashboard.Interfaces;
using Accounting.Core.Enums;
using Accounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Provides aggregated operational data for manager dashboards.
/// All aggregations are performed at the database level via EF Core projections.
/// Only Confirmed purchase invoices and Completed sales invoices count toward financial totals.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly AccountingDbContext _db;

    public DashboardService(AccountingDbContext db) => _db = db;

    // ─── Summary ──────────────────────────────────────────────────────────────

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        DateOnly from, DateOnly to,
        Guid? branchId = null, Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var sales      = await GetSalesSummaryAsync(from, to, branchId, warehouseId, ct);
        var purchases  = await GetPurchaseSummaryAsync(from, to, branchId, warehouseId, ct);
        var inventory  = await GetInventorySummaryAsync(warehouseId, ct);
        var alerts     = await GetAlertSummaryAsync(ct);

        return new DashboardSummaryDto(sales, purchases, inventory, alerts);
    }

    // ─── Sales summary ────────────────────────────────────────────────────────

    private async Task<SalesSummaryDto> GetSalesSummaryAsync(
        DateOnly from, DateOnly to,
        Guid? branchId, Guid? warehouseId,
        CancellationToken ct)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var q = _db.SalesInvoices
            .Where(s => s.Status == SalesInvoiceStatus.Completed
                     && s.SaleDate >= fromUtc && s.SaleDate <= toUtc);

        if (branchId.HasValue)    q = q.Where(s => s.BranchId    == branchId.Value);
        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);

        var result = await q
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count    = g.Count(),
                Revenue  = g.Sum(s => (double)s.TotalAmount),
                Tax      = g.Sum(s => (double)s.TaxAmount),
                Discount = g.Sum(s => (double)s.DiscountAmount),
            })
            .FirstOrDefaultAsync(ct);

        var count   = result?.Count   ?? 0;
        var revenue = (decimal)(result?.Revenue ?? 0d);

        return new SalesSummaryDto(
            InvoiceCount:     count,
            TotalRevenue:     revenue,
            TotalTax:         (decimal)(result?.Tax      ?? 0d),
            TotalDiscount:    (decimal)(result?.Discount ?? 0d),
            AverageOrderValue: count > 0 ? revenue / count : 0m
        );
    }

    // ─── Purchase summary ─────────────────────────────────────────────────────

    private async Task<PurchaseSummaryDto> GetPurchaseSummaryAsync(
        DateOnly from, DateOnly to,
        Guid? branchId, Guid? warehouseId,
        CancellationToken ct)
    {
        var q = _db.PurchaseInvoices
            .Where(p => p.Status == PurchaseInvoiceStatus.Confirmed
                     && p.InvoiceDate >= from && p.InvoiceDate <= to);

        if (branchId.HasValue)    q = q.Where(p => p.BranchId    == branchId.Value);
        if (warehouseId.HasValue) q = q.Where(p => p.WarehouseId == warehouseId.Value);

        var result = await q
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count      = g.Count(),
                TotalCost  = g.Sum(p => (double)p.TotalAmount),
                Tax        = g.Sum(p => (double)p.TaxAmount),
                Discount   = g.Sum(p => (double)p.DiscountAmount),
                BalanceDue = g.Sum(p => (double)(p.TotalAmount - p.PaidAmount)),
            })
            .FirstOrDefaultAsync(ct);

        return new PurchaseSummaryDto(
            InvoiceCount:  result?.Count                    ?? 0,
            TotalCost:     (decimal)(result?.TotalCost  ?? 0d),
            TotalTax:      (decimal)(result?.Tax        ?? 0d),
            TotalDiscount: (decimal)(result?.Discount   ?? 0d),
            BalanceDue:    (decimal)(result?.BalanceDue ?? 0d)
        );
    }

    // ─── Alert summary ────────────────────────────────────────────────────────

    private async Task<AlertSummaryDto> GetAlertSummaryAsync(CancellationToken ct)
    {
        var counts = await _db.Alerts
            .Where(a => a.Status == AlertStatus.Active || a.Status == AlertStatus.Acknowledged)
            .GroupBy(a => a.AlertType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(AlertType t) => counts.FirstOrDefault(c => c.Type == t)?.Count ?? 0;

        return new AlertSummaryDto(
            TotalActive:   counts.Sum(c => c.Count),
            LowStock:      Get(AlertType.LowStock),
            NearExpiry:    Get(AlertType.NearExpiry),
            ExpiredStock:  Get(AlertType.ExpiredStock),
            BatchRecalled: Get(AlertType.BatchRecalled)
        );
    }

    // ─── Inventory summary ────────────────────────────────────────────────────

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(
        Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var q = _db.StockBalances.Include(b => b.Item).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(b => b.WarehouseId == warehouseId.Value);

        var result = await q
            .GroupBy(_ => 1)
            .Select(g => new
            {
                DistinctItems      = g.Select(b => b.ItemId).Distinct().Count(),
                DistinctWarehouses = g.Select(b => b.WarehouseId).Distinct().Count(),
                TotalQty           = g.Sum(b => (double)b.QuantityOnHand),
                LowStock           = g.Count(b => b.QuantityOnHand > 0 && b.QuantityOnHand <= b.Item.ReorderLevel),
                OutOfStock         = g.Count(b => b.QuantityOnHand <= 0),
            })
            .FirstOrDefaultAsync(ct);

        return new InventorySummaryDto(
            DistinctItems:       result?.DistinctItems      ?? 0,
            DistinctWarehouses:  result?.DistinctWarehouses ?? 0,
            TotalQuantityOnHand: (decimal)(result?.TotalQty ?? 0d),
            LowStockItemCount:   result?.LowStock           ?? 0,
            OutOfStockItemCount: result?.OutOfStock         ?? 0
        );
    }

    // ─── Sales trend ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<DailySalesDto>> GetSalesTrendAsync(
        DateOnly from, DateOnly to,
        Guid? branchId = null, Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var q = _db.SalesInvoices
            .Where(s => s.Status == SalesInvoiceStatus.Completed
                     && s.SaleDate >= fromUtc && s.SaleDate <= toUtc);

        if (branchId.HasValue)    q = q.Where(s => s.BranchId    == branchId.Value);
        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);

        var rows = await q
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new
            {
                Date    = g.Key,
                Count   = g.Count(),
                Revenue = g.Sum(s => (double)s.TotalAmount),
            })
            .OrderBy(r => r.Date)
            .ToListAsync(ct);

        return rows
            .Select(r => new DailySalesDto(
                DateOnly.FromDateTime(r.Date),
                r.Count,
                (decimal)r.Revenue))
            .ToList();
    }

    // ─── Top-selling items ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TopSellingItemDto>> GetTopSellingItemsAsync(
        DateOnly from, DateOnly to,
        int top = 10,
        Guid? branchId = null, Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc   = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var invoiceQ = _db.SalesInvoices
            .Where(s => s.Status == SalesInvoiceStatus.Completed
                     && s.SaleDate >= fromUtc && s.SaleDate <= toUtc);

        if (branchId.HasValue)    invoiceQ = invoiceQ.Where(s => s.BranchId    == branchId.Value);
        if (warehouseId.HasValue) invoiceQ = invoiceQ.Where(s => s.WarehouseId == warehouseId.Value);

        var rows = await _db.SalesInvoiceLines
            .Where(l => invoiceQ.Select(s => s.Id).Contains(l.SalesInvoiceId))
            .GroupBy(l => new { l.ItemId, l.Item.Name, l.Item.SKU })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.Name,
                g.Key.SKU,
                QuantitySold = g.Sum(l => (double)l.Quantity),
                Revenue      = g.Sum(l => (double)l.LineTotal),
            })
            .OrderByDescending(r => r.QuantitySold)
            .Take(top)
            .ToListAsync(ct);

        return rows
            .Select(r => new TopSellingItemDto(
                r.ItemId, r.Name, r.SKU,
                (decimal)r.QuantitySold, (decimal)r.Revenue))
            .ToList();
    }

    // ─── Expiry risk ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ExpiryRiskDto>> GetExpiryRiskAsync(
        int withinDays = 30,
        Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var today     = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(withinDays);

        var q = _db.ItemBatches
            .Include(b => b.Item)
            .Include(b => b.Warehouse)
            .Where(b => b.ExpiryDate.HasValue
                     && b.ExpiryDate.Value <= threshold
                     && b.AvailableQuantity > 0
                     && b.Status == BatchStatus.Active);

        if (warehouseId.HasValue) q = q.Where(b => b.WarehouseId == warehouseId.Value);

        var rows = await q
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new
            {
                b.Id,
                b.BatchNumber,
                b.ItemId,
                ItemName      = b.Item.Name,
                b.WarehouseId,
                WarehouseName = b.Warehouse.Name,
                b.ExpiryDate,
                b.AvailableQuantity,
            })
            .ToListAsync(ct);

        return rows.Select(r => new ExpiryRiskDto(
            BatchId:           r.Id,
            BatchNumber:       r.BatchNumber,
            ItemId:            r.ItemId,
            ItemName:          r.ItemName,
            WarehouseId:       r.WarehouseId,
            WarehouseName:     r.WarehouseName,
            ExpiryDate:        r.ExpiryDate!.Value,
            DaysUntilExpiry:   r.ExpiryDate!.Value.DayNumber - today.DayNumber,
            AvailableQuantity: r.AvailableQuantity,
            IsExpired:         r.ExpiryDate!.Value < today
        )).ToList();
    }
}

