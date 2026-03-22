namespace Accounting.Core.Entities;

/// <summary>
/// Canonical allocation record linking a sales invoice line to one specific batch
/// (or null for non-batch items). A single SalesInvoiceLine may have multiple
/// allocations when FEFO spans more than one batch.
///
/// Design rationale:
///   SalesInvoiceLine.ItemBatchId is insufficient for multi-batch sales.
///   This table is the authoritative source for "which batch was consumed in this sale."
///   Each allocation maps 1:1 to one StockMovement with the same batch, quantity, and invoice reference.
/// </summary>
public class SalesInvoiceLineAllocation : BaseEntity
{
    public Guid SalesInvoiceLineId { get; set; }
    public SalesInvoiceLine SalesInvoiceLine { get; set; } = null!;

    /// <summary>Null for non-batch-tracked items.</summary>
    public Guid? ItemBatchId { get; set; }
    public ItemBatch? ItemBatch { get; set; }

    /// <summary>Quantity consumed from this batch for this line.</summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Batch cost at time of sale. Snapshot for future FIFO/weighted-average costing.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Denormalized expiry snapshot. Avoids joins in expiry-approaching reports.
    /// </summary>
    public DateOnly? ExpiryDateSnapshot { get; set; }
}

