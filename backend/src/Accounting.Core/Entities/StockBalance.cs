namespace Accounting.Core.Entities;

/// <summary>
/// Performance-optimized summary of current stock per item per warehouse.
/// MUST always be consistent with the sum of StockMovements for the same item/warehouse pair.
/// Updated atomically in the same transaction as each StockMovement insert.
/// </summary>
public class StockBalance : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public decimal QuantityOnHand { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

