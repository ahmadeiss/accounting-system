using Accounting.Application.Dashboard.DTOs;

namespace Accounting.Application.Dashboard.Interfaces;

/// <summary>
/// Read-only service that provides aggregated operational data for manager dashboards.
///
/// Rules:
///   - Only Confirmed purchase invoices and Completed sales invoices count toward financial totals.
///   - Inventory figures are point-in-time (not period-bound).
///   - All date parameters are treated as UTC dates.
///   - BranchId / WarehouseId filters are optional; null means "all branches/warehouses".
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Returns a single-call operational snapshot combining sales, purchases,
    /// inventory, and alert summaries for the given period.
    /// </summary>
    Task<DashboardSummaryDto> GetSummaryAsync(
        DateOnly    from,
        DateOnly    to,
        Guid?       branchId      = null,
        Guid?       warehouseId   = null,
        CancellationToken ct      = default);

    /// <summary>
    /// Returns current inventory snapshot (point-in-time).
    /// </summary>
    Task<InventorySummaryDto> GetInventorySummaryAsync(
        Guid?             warehouseId = null,
        CancellationToken ct          = default);

    /// <summary>
    /// Returns daily sales revenue and invoice count for the given period.
    /// Suitable for rendering a time-series chart.
    /// </summary>
    Task<IReadOnlyList<DailySalesDto>> GetSalesTrendAsync(
        DateOnly          from,
        DateOnly          to,
        Guid?             branchId    = null,
        Guid?             warehouseId = null,
        CancellationToken ct          = default);

    /// <summary>
    /// Returns the top N items by quantity sold in the given period.
    /// </summary>
    Task<IReadOnlyList<TopSellingItemDto>> GetTopSellingItemsAsync(
        DateOnly          from,
        DateOnly          to,
        int               top         = 10,
        Guid?             branchId    = null,
        Guid?             warehouseId = null,
        CancellationToken ct          = default);

    /// <summary>
    /// Returns batches that are expired or expiring within the given number of days.
    /// Ordered by ExpiryDate ascending (most urgent first).
    /// </summary>
    Task<IReadOnlyList<ExpiryRiskDto>> GetExpiryRiskAsync(
        int               withinDays  = 30,
        Guid?             warehouseId = null,
        CancellationToken ct          = default);
}

