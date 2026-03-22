using Accounting.Core.ValueObjects;

namespace Accounting.Core.Interfaces;

/// <summary>
/// Performs FEFO batch allocation for sales.
///
/// FEFO rule:
///   For expiry-tracked items: earliest ExpiryDate is consumed first.
///   For batch-tracked non-expiry items: oldest batch (by CreatedAt) is consumed first.
///   For non-batch items: caller should use ValidateSufficientStockAsync directly; this
///     service should not be called for non-batch items.
///
/// Allocation spans multiple batches automatically when a single batch is insufficient.
/// Throws <see cref="Accounting.Core.Exceptions.InsufficientStockException"/> if the
/// total available quantity across all sellable batches is less than the requested amount.
/// </summary>
public interface IBatchSelectionService
{
    /// <summary>
    /// Allocates <paramref name="quantityNeeded"/> from sellable FEFO-ordered batches.
    /// Returns one <see cref="BatchAllocation"/> per batch consumed.
    /// The sum of all allocation quantities equals <paramref name="quantityNeeded"/>.
    /// </summary>
    Task<IReadOnlyList<BatchAllocation>> AllocateFEFOAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantityNeeded,
        CancellationToken ct = default);
}

