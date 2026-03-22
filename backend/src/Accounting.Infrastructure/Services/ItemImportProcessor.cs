using Accounting.Application.Import.DTOs;
using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Excel;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Validates and (optionally) persists a single Item row from an Excel import.
///
/// Expected columns (case-insensitive):
///   Name*, SKU*, Barcode, CategoryName*, UnitName*,
///   CostPrice*, SalePrice*, ReorderLevel, TrackBatch, TrackExpiry, MinExpiryDaysBeforeSale
///
/// Validation rules:
///   - Name, SKU, CategoryName, UnitName are required.
///   - SKU must be unique across existing items AND within the current import batch.
///   - CostPrice and SalePrice must be non-negative decimals.
///   - If TrackExpiry = true then TrackBatch is forced to true.
///   - CategoryName and UnitName must match existing records (case-insensitive).
/// </summary>
public sealed class ItemImportProcessor
{
    private readonly AccountingDbContext _context;

    public ItemImportProcessor(AccountingDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Validates a row. Returns null if valid, or an error message if invalid.
    /// Does NOT persist anything.
    /// </summary>
    public async Task<string?> ValidateAsync(
        ExcelRow row,
        HashSet<string> seenSkusInBatch,
        CancellationToken ct)
    {
        var name = row.Get("Name");
        var sku = row.Get("SKU");
        var categoryName = row.Get("CategoryName");
        var unitName = row.Get("UnitName");

        if (string.IsNullOrWhiteSpace(name))
            return "Name is required.";
        if (string.IsNullOrWhiteSpace(sku))
            return "SKU is required.";
        if (string.IsNullOrWhiteSpace(categoryName))
            return "CategoryName is required.";
        if (string.IsNullOrWhiteSpace(unitName))
            return "UnitName is required.";

        if (!row.TryGetDecimal("CostPrice", out var costPrice) || costPrice < 0)
            return "CostPrice must be a non-negative number.";
        if (!row.TryGetDecimal("SalePrice", out var salePrice) || salePrice < 0)
            return "SalePrice must be a non-negative number.";

        // Duplicate SKU within this import batch
        if (!seenSkusInBatch.Add(sku.ToUpperInvariant()))
            return $"Duplicate SKU '{sku}' within this import file.";

        // Duplicate SKU in database
        bool skuExists = await _context.Items.AnyAsync(i => i.SKU == sku, ct);
        if (skuExists)
            return $"SKU '{sku}' already exists in the system.";

        // Category must exist
        bool categoryExists = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == categoryName.ToLower(), ct);
        if (!categoryExists)
            return $"Category '{categoryName}' not found.";

        // Unit must exist
        bool unitExists = await _context.Units
            .AnyAsync(u => u.Name.ToLower() == unitName.ToLower(), ct);
        if (!unitExists)
            return $"Unit '{unitName}' not found.";

        return null; // valid
    }

    /// <summary>
    /// Persists a validated row as a new Item. Caller must call SaveChanges.
    /// </summary>
    public async Task<Item> PersistAsync(ExcelRow row, CancellationToken ct)
    {
        var sku = row.Get("SKU");
        var categoryName = row.Get("CategoryName");
        var unitName = row.Get("UnitName");

        var category = await _context.Categories
            .FirstAsync(c => c.Name.ToLower() == categoryName.ToLower(), ct);
        var unit = await _context.Units
            .FirstAsync(u => u.Name.ToLower() == unitName.ToLower(), ct);

        row.TryGetDecimal("CostPrice", out var costPrice);
        row.TryGetDecimal("SalePrice", out var salePrice);
        row.TryGetDecimal("ReorderLevel", out var reorderLevel);
        row.TryGetBool("TrackBatch", out var trackBatch);
        row.TryGetBool("TrackExpiry", out var trackExpiry);
        row.TryGetDecimal("MinExpiryDaysBeforeSale", out var minExpiry);

        if (trackExpiry) trackBatch = true; // enforce invariant

        var item = new Item
        {
            Name = row.Get("Name"),
            SKU = sku,
            Barcode = string.IsNullOrWhiteSpace(row.Get("Barcode")) ? null : row.Get("Barcode"),
            CategoryId = category.Id,
            UnitId = unit.Id,
            CostPrice = costPrice,
            SalePrice = salePrice,
            ReorderLevel = reorderLevel,
            TrackBatch = trackBatch,
            TrackExpiry = trackExpiry,
            MinExpiryDaysBeforeSale = (int)minExpiry,
            IsActive = true
        };

        _context.Items.Add(item);
        return item;
    }
}

