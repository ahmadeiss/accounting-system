namespace Accounting.Core.Entities;

public class PurchaseInvoiceLine : BaseEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Batch number. Required at invoice level when Item.TrackBatch = true.</summary>
    public string? BatchNumber { get; set; }
    public DateOnly? ProductionDate { get; set; }

    /// <summary>Required at invoice level when Item.TrackExpiry = true.</summary>
    public DateOnly? ExpiryDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Set on confirmation. Points to the ItemBatch record created or resolved for this line.
    /// Null for items that do not track batches.
    /// </summary>
    public Guid? ItemBatchId { get; set; }
    public ItemBatch? ItemBatch { get; set; }

    /// <summary>Computed effective discount amount for this line.</summary>
    public decimal DiscountAmount => Math.Round(Quantity * UnitCost * DiscountPercent / 100, 4);

    /// <summary>Computed effective tax amount for this line (applied after discount).</summary>
    public decimal TaxAmount => Math.Round((Quantity * UnitCost - DiscountAmount) * TaxPercent / 100, 4);
}

