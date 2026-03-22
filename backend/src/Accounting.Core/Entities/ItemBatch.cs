using Accounting.Core.Enums;

namespace Accounting.Core.Entities;

/// <summary>
/// Represents a received batch/lot of an item in a specific warehouse.
/// Used for FEFO (First Expiry First Out) selling and expiry tracking.
/// </summary>
public class ItemBatch : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly? ProductionDate { get; set; }

    /// <summary>Required when Item.TrackExpiry = true. Enforced at receiving time.</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>Quantity received from the purchase — immutable after creation.</summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>
    /// Quantity currently available. Decremented on sales and adjustments.
    /// Must never be manually set — only updated via stock movement recording.
    /// </summary>
    public decimal AvailableQuantity { get; set; }

    public decimal CostPerUnit { get; set; }

    public BatchStatus Status { get; set; } = BatchStatus.Active;

    public string? Notes { get; set; }

    // Navigation
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<SalesInvoiceLine> SalesLines { get; set; } = new List<SalesInvoiceLine>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    /// <summary>
    /// Returns true if this batch has passed its expiry date as of the given date.
    /// </summary>
    public bool IsExpiredOn(DateOnly checkDate) =>
        ExpiryDate.HasValue && ExpiryDate.Value < checkDate;

    /// <summary>
    /// Returns true if this batch will expire within the given number of days.
    /// </summary>
    public bool IsNearExpiryOn(DateOnly checkDate, int withinDays) =>
        ExpiryDate.HasValue &&
        ExpiryDate.Value >= checkDate &&
        ExpiryDate.Value <= checkDate.AddDays(withinDays);

    /// <summary>
    /// Returns true if this batch is sellable: Active, not expired, has available quantity.
    /// </summary>
    public bool IsSellable(DateOnly saleDate, int minDaysBeforeExpiry) =>
        Status == BatchStatus.Active &&
        AvailableQuantity > 0 &&
        (!ExpiryDate.HasValue || ExpiryDate.Value >= saleDate.AddDays(minDaysBeforeExpiry));
}

