namespace Accounting.Core.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
}

