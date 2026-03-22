namespace Accounting.Core.Entities;

public class SalesInvoiceLine : BaseEntity
{
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    /// <summary>
    /// Required for expiry-tracked items. Set by FEFO batch selection at service layer.
    /// Records exactly which batch was consumed for full traceability.
    /// </summary>
    public Guid? ItemBatchId { get; set; }
    public ItemBatch? ItemBatch { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Canonical allocation records created on confirmation.
    /// For non-batch items: one record with ItemBatchId = null.
    /// For batch items: one record per batch consumed (multi-batch FEFO).
    /// </summary>
    public ICollection<SalesInvoiceLineAllocation> Allocations { get; set; }
        = new List<SalesInvoiceLineAllocation>();
}

