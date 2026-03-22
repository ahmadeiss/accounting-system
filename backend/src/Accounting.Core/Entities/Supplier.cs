namespace Accounting.Core.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Average lead time in days. Used for future AI procurement forecasting.
    /// Populated from historical data or manual entry.
    /// </summary>
    public int LeadTimeDays { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
}

