using Accounting.Core.Enums;

namespace Accounting.Core.Entities;

/// <summary>
/// Immutable ledger record of every stock change.
/// This table is the source of truth for all stock quantity history.
/// No stock can change without a movement record.
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    /// <summary>Set for batch-tracked items. Null for non-tracked items.</summary>
    public Guid? ItemBatchId { get; set; }
    public ItemBatch? ItemBatch { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public StockMovementType MovementType { get; set; }

    /// <summary>
    /// Positive = stock in (purchase, transfer in, opening, return).
    /// Negative = stock out (sale, transfer out, adjustment loss).
    /// </summary>
    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    /// <summary>The document type that caused this movement. Example: "PurchaseInvoice", "SalesInvoice"</summary>
    public string ReferenceType { get; set; } = string.Empty;

    /// <summary>The ID of the reference document.</summary>
    public Guid? ReferenceId { get; set; }

    public string? Notes { get; set; }

    /// <summary>When the movement occurred (may differ from CreatedAt for backdated entries).</summary>
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
}

