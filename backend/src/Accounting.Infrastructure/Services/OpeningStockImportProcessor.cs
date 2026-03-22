using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Excel;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Validates and (optionally) persists a single Opening Stock row from an Excel import.
///
/// Expected columns (case-insensitive):
///   SKU*, Quantity*, CostPerUnit*, BatchNumber (required if TrackBatch),
///   ExpiryDate (required if TrackExpiry), ProductionDate
///
/// Validation rules:
///   - SKU must exist in the items table.
///   - Quantity must be > 0.
///   - CostPerUnit must be >= 0.
///   - If item.TrackBatch = true, BatchNumber is required.
///   - If item.TrackExpiry = true, ExpiryDate is required and must be a future date.
///   - Duplicate (SKU + BatchNumber) within the same import is rejected.
///
/// Stock integrity:
///   All stock mutations go through IStockService.RecordMovementAsync with
///   StockMovementType.Opening. This ensures StockBalance and ItemBatch.AvailableQuantity
///   are updated atomically and the full audit trail is preserved.
/// </summary>
public sealed class OpeningStockImportProcessor
{
    private readonly AccountingDbContext _context;
    private readonly IStockService _stock;

    public OpeningStockImportProcessor(AccountingDbContext context, IStockService stock)
    {
        _context = context;
        _stock = stock;
    }

    /// <summary>
    /// Validates a row. Returns null if valid, or an error message if invalid.
    /// Does NOT persist anything.
    /// </summary>
    public async Task<(string? Error, Item? Item)> ValidateAsync(
        ExcelRow row,
        Guid warehouseId,
        HashSet<string> seenBatchKeysInBatch,
        CancellationToken ct)
    {
        var sku = row.Get("SKU");
        if (string.IsNullOrWhiteSpace(sku))
            return ("SKU is required.", null);

        if (!row.TryGetDecimal("Quantity", out var qty) || qty <= 0)
            return ("Quantity must be a positive number.", null);

        if (!row.TryGetDecimal("CostPerUnit", out var cost) || cost < 0)
            return ("CostPerUnit must be a non-negative number.", null);

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.SKU == sku, ct);
        if (item is null)
            return ($"SKU '{sku}' not found in the system.", null);

        var batchNumber = row.Get("BatchNumber");

        if (item.TrackBatch && string.IsNullOrWhiteSpace(batchNumber))
            return ($"BatchNumber is required for batch-tracked item '{sku}'.", null);

        if (item.TrackExpiry)
        {
            if (!row.TryGetDateOnly("ExpiryDate", out var expiry))
                return ($"ExpiryDate is required for expiry-tracked item '{sku}'.", null);
            if (expiry <= DateOnly.FromDateTime(DateTime.UtcNow))
                return ($"ExpiryDate must be a future date for item '{sku}'.", null);
        }

        // Duplicate (SKU + BatchNumber) within this import
        var batchKey = $"{sku.ToUpperInvariant()}|{batchNumber?.ToUpperInvariant() ?? ""}";
        if (!seenBatchKeysInBatch.Add(batchKey))
            return ($"Duplicate SKU+BatchNumber combination '{sku}'/'{batchNumber}' in this file.", null);

        return (null, item);
    }

    /// <summary>
    /// Persists a validated opening stock row.
    /// Creates an ItemBatch (if TrackBatch) and records a stock movement via IStockService.
    /// Caller must call SaveChanges after this method.
    /// </summary>
    public async Task PersistAsync(
        ExcelRow row,
        Item item,
        Guid warehouseId,
        Guid createdById,
        Guid importJobId,
        CancellationToken ct)
    {
        row.TryGetDecimal("Quantity", out var qty);
        row.TryGetDecimal("CostPerUnit", out var cost);
        var batchNumber = row.Get("BatchNumber");

        Guid? batchId = null;

        if (item.TrackBatch)
        {
            row.TryGetDateOnly("ExpiryDate", out var expiry);
            row.TryGetDateOnly("ProductionDate", out var prodDate);

            var batch = new ItemBatch
            {
                ItemId = item.Id,
                WarehouseId = warehouseId,
                BatchNumber = batchNumber,
                ReceivedQuantity = qty,
                AvailableQuantity = qty,
                CostPerUnit = cost,
                ExpiryDate = item.TrackExpiry ? expiry : null,
                ProductionDate = prodDate == default ? null : prodDate,
                Status = BatchStatus.Active,
                Notes = $"Opening stock import job {importJobId}"
            };
            _context.ItemBatches.Add(batch);
            batchId = batch.Id;
        }

        await _stock.RecordMovementAsync(
            itemId: item.Id,
            warehouseId: warehouseId,
            quantity: qty,
            movementType: StockMovementType.Opening,
            referenceType: "ImportJob",
            referenceId: importJobId,
            itemBatchId: batchId,
            unitCost: cost,
            notes: $"Opening stock import",
            createdById: createdById,
            ct: ct);
    }
}

