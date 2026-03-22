using Accounting.Core.Exceptions;
using Accounting.Core.Interfaces;
using Accounting.Core.ValueObjects;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// FEFO batch allocation engine.
///
/// Algorithm:
///   1. Retrieve sellable batches via IStockService.GetFefoBatchesAsync().
///      That method already enforces:
///        - Status == Active
///        - AvailableQuantity > 0
///        - ExpiryDate is null OR >= today  (expired batches excluded)
///      And orders by: (ExpiryDate IS NULL, ExpiryDate ASC, CreatedAt ASC)
///      i.e. expiry-tracked items consume earliest-expiry first;
///           batch-tracked non-expiry items consume oldest-created first (FIFO on receipt).
///
///   2. Walk the ordered batches, consuming as much as possible from each until the
///      required quantity is satisfied.
///
///   3. If total available < required → InsufficientStockException.
///
/// This service ONLY decides allocation. It does NOT write to the database.
/// All writes (StockMovement, SalesInvoiceLineAllocation) are the caller's responsibility.
/// </summary>
public class BatchSelectionService : IBatchSelectionService
{
    private readonly IStockService _stock;

    public BatchSelectionService(IStockService stock)
    {
        _stock = stock;
    }

    public async Task<IReadOnlyList<BatchAllocation>> AllocateFEFOAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantityNeeded,
        CancellationToken ct = default)
    {
        if (quantityNeeded <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantityNeeded), "Quantity must be positive.");

        // Fetch FEFO-ordered sellable batches (non-expired, active, qty > 0)
        var batches = await _stock.GetFefoBatchesAsync(itemId, warehouseId, ct);

        var allocations = new List<BatchAllocation>();
        var remaining = quantityNeeded;

        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            var take = Math.Min(remaining, batch.AvailableQuantity);

            allocations.Add(new BatchAllocation(
                BatchId: batch.Id,
                BatchNumber: batch.BatchNumber,
                Quantity: take,
                UnitCost: batch.CostPerUnit,
                ExpiryDate: batch.ExpiryDate));

            remaining -= take;
        }

        if (remaining > 0)
        {
            // Compute total available for the error message
            var totalAvailable = quantityNeeded - remaining;
            throw new InsufficientStockException(itemId, quantityNeeded, totalAvailable);
        }

        return allocations;
    }
}

