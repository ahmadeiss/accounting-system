using Accounting.Core.Entities;
using Accounting.Core.Enums;

namespace Accounting.Core.Interfaces;

/// <summary>
/// Core stock operations. All stock changes MUST go through this service.
/// Direct manipulation of ItemBatch.AvailableQuantity or StockBalance.QuantityOnHand
/// outside this service is strictly forbidden.
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Records a stock movement and atomically updates StockBalance and ItemBatch.AvailableQuantity.
    /// NOTE: Does NOT call SaveChanges. The caller is responsible for committing the transaction.
    /// </summary>
    Task RecordMovementAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        StockMovementType movementType,
        string referenceType,
        Guid? referenceId,
        Guid? itemBatchId,
        decimal unitCost,
        string? notes,
        Guid createdById,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the current on-hand quantity for an item in a warehouse.
    /// </summary>
    Task<decimal> GetStockBalanceAsync(Guid itemId, Guid warehouseId, CancellationToken ct = default);

    /// <summary>
    /// Returns sellable batches for an item in a warehouse, ordered by FEFO (earliest expiry first).
    /// Only returns batches that are Active, not expired, and have available quantity.
    /// </summary>
    Task<IReadOnlyList<ItemBatch>> GetFefoBatchesAsync(Guid itemId, Guid warehouseId, CancellationToken ct = default);

    /// <summary>
    /// Checks if sufficient stock exists for a sale. Throws InsufficientStockException if not.
    /// </summary>
    Task ValidateSufficientStockAsync(Guid itemId, Guid warehouseId, decimal requiredQuantity, CancellationToken ct = default);
}

