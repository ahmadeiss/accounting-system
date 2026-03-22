using Accounting.Application.Import.DTOs;

namespace Accounting.Application.Import.Services;

/// <summary>
/// Orchestrates Excel import jobs for item master and opening stock.
///
/// Dry-run mode: validates every row and returns a full report without persisting
/// any data (no ImportJob row, no stock movements, no items created).
///
/// Commit mode: validates all rows first; if any row fails the entire job is
/// marked PartialSuccess and only valid rows are committed (row-level commit
/// strategy). Each committed row is wrapped in its own SaveChanges call so a
/// single bad row cannot roll back already-committed rows.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Imports items from an Excel file.
    /// Columns expected: Name*, SKU*, Barcode, CategoryName*, UnitName*,
    ///                   CostPrice*, SalePrice*, ReorderLevel, TrackBatch, TrackExpiry.
    /// </summary>
    Task<ImportResult> ImportItemsAsync(
        ImportItemsRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Imports opening stock from an Excel file.
    /// Columns expected: SKU*, Quantity*, CostPerUnit*, BatchNumber (if TrackBatch),
    ///                   ExpiryDate (if TrackExpiry).
    /// All stock movements use StockMovementType.Opening and flow through IStockService.
    /// </summary>
    Task<ImportResult> ImportOpeningStockAsync(
        ImportOpeningStockRequest request,
        CancellationToken ct = default);

    /// <summary>Returns the persisted ImportJob result for a previously run job.</summary>
    Task<ImportResult?> GetJobResultAsync(Guid jobId, CancellationToken ct = default);
}

