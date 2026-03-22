using Accounting.Core.Enums;

namespace Accounting.Core.Entities;

public class SalesInvoice : BaseEntity
{
    /// <summary>System-generated unique number. Example: SAL-2024-00001</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Optional: walk-in customers may not be registered.</summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Draft;

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }

    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    // Navigation
    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();

    public decimal ChangeAmount => PaidAmount - TotalAmount;
}

