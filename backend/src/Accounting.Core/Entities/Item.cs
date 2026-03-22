namespace Accounting.Core.Entities;

public class Item : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal ReorderLevel { get; set; }

    /// <summary>
    /// When true, purchase receiving must include a batch number for this item.
    /// </summary>
    public bool TrackBatch { get; set; }

    /// <summary>
    /// When true, purchase receiving must include an expiry date.
    /// Implies TrackBatch = true (enforced at service layer).
    /// </summary>
    public bool TrackExpiry { get; set; }

    /// <summary>
    /// Minimum days remaining before expiry for this item to be sold.
    /// Example: 7 means the system blocks sale if batch expires within 7 days.
    /// </summary>
    public int MinExpiryDaysBeforeSale { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<ItemBatch> Batches { get; set; } = new List<ItemBatch>();
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<PurchaseInvoiceLine> PurchaseLines { get; set; } = new List<PurchaseInvoiceLine>();
    public ICollection<SalesInvoiceLine> SalesLines { get; set; } = new List<SalesInvoiceLine>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}

