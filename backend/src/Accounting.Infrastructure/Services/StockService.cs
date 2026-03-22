using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Exceptions;
using Accounting.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Accounting.Infrastructure.Data;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Core stock ledger service. All stock mutations go through here.
/// Enforces: FEFO ordering, expiry blocking, atomic balance updates.
/// </summary>
public class StockService : IStockService
{
    private readonly AccountingDbContext _context;

    public StockService(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task RecordMovementAsync(
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
        CancellationToken ct = default)
    {
        // 1. Write the immutable ledger entry
        var movement = new StockMovement
        {
            ItemId = itemId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            MovementType = movementType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ItemBatchId = itemBatchId,
            UnitCost = unitCost,
            Notes = notes,
            MovementDate = DateTime.UtcNow,
            CreatedById = createdById
        };
        await _context.StockMovements.AddAsync(movement, ct);

        // 2. Update batch available quantity (if batch-tracked)
        if (itemBatchId.HasValue)
        {
            var batch = await _context.ItemBatches.FindAsync(new object[] { itemBatchId.Value }, ct)
                ?? throw new NotFoundException(nameof(ItemBatch), itemBatchId.Value);

            batch.AvailableQuantity += quantity; // quantity is negative for outbound movements

            if (batch.AvailableQuantity < 0)
                throw new InsufficientStockException(itemId, Math.Abs(quantity), batch.AvailableQuantity + Math.Abs(quantity));

            if (batch.AvailableQuantity == 0)
                batch.Status = BatchStatus.Depleted;
        }

        // 3. Upsert StockBalance (atomic with the movement)
        var balance = await _context.StockBalances
            .FirstOrDefaultAsync(b => b.ItemId == itemId && b.WarehouseId == warehouseId, ct);

        if (balance is null)
        {
            balance = new StockBalance { ItemId = itemId, WarehouseId = warehouseId, QuantityOnHand = 0 };
            await _context.StockBalances.AddAsync(balance, ct);
        }

        balance.QuantityOnHand += quantity;
        balance.LastUpdated = DateTime.UtcNow;

        if (balance.QuantityOnHand < 0)
            throw new InsufficientStockException(itemId, Math.Abs(quantity), balance.QuantityOnHand + Math.Abs(quantity));
    }

    public async Task<decimal> GetStockBalanceAsync(Guid itemId, Guid warehouseId, CancellationToken ct = default)
    {
        var balance = await _context.StockBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ItemId == itemId && b.WarehouseId == warehouseId, ct);

        return balance?.QuantityOnHand ?? 0;
    }

    public async Task<IReadOnlyList<ItemBatch>> GetFefoBatchesAsync(
        Guid itemId, Guid warehouseId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // FEFO: order by ExpiryDate ascending (nulls last = non-expiry items come after expiry-tracked)
        return await _context.ItemBatches
            .AsNoTracking()
            .Where(b =>
                b.ItemId == itemId &&
                b.WarehouseId == warehouseId &&
                b.Status == BatchStatus.Active &&
                b.AvailableQuantity > 0 &&
                (!b.ExpiryDate.HasValue || b.ExpiryDate.Value >= today))
            .OrderBy(b => b.ExpiryDate == null ? 1 : 0)
            .ThenBy(b => b.ExpiryDate)
            .ThenBy(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task ValidateSufficientStockAsync(
        Guid itemId, Guid warehouseId, decimal requiredQuantity, CancellationToken ct = default)
    {
        var available = await GetStockBalanceAsync(itemId, warehouseId, ct);
        if (available < requiredQuantity)
            throw new InsufficientStockException(itemId, requiredQuantity, available);
    }
}

