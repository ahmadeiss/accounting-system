using Accounting.Core.Enums;

namespace Accounting.Core.Entities;

public class PurchaseInvoice : BaseEntity
{
    /// <summary>System-generated unique number. Example: PUR-2024-00001</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }

    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Draft;

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public string? Notes { get; set; }

    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    // Navigation
    public ICollection<PurchaseInvoiceLine> Lines { get; set; } = new List<PurchaseInvoiceLine>();

    public decimal BalanceDue => TotalAmount - PaidAmount;
}

