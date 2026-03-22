using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(300);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(300);
    }
}

public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("purchase_invoices");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.InvoiceNumber).IsUnique();
        builder.Property(p => p.Status).HasConversion<string>();
        builder.Property(p => p.SubTotal).HasPrecision(18, 4);
        builder.Property(p => p.TaxAmount).HasPrecision(18, 4);
        builder.Property(p => p.DiscountAmount).HasPrecision(18, 4);
        builder.Property(p => p.TotalAmount).HasPrecision(18, 4);
        builder.Property(p => p.PaidAmount).HasPrecision(18, 4);

        // Dashboard: filter by status + date range
        builder.HasIndex(p => new { p.Status, p.InvoiceDate })
            .HasDatabaseName("IX_purchase_invoices_Status_InvoiceDate");

        builder.HasOne(p => p.Supplier).WithMany(s => s.PurchaseInvoices)
            .HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.CreatedBy).WithMany()
            .HasForeignKey(p => p.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseInvoiceLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLine>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceLine> builder)
    {
        builder.ToTable("purchase_invoice_lines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);

        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);
        builder.Property(l => l.TaxPercent).HasPrecision(5, 2);

        builder.HasOne(l => l.PurchaseInvoice).WithMany(p => p.Lines)
            .HasForeignKey(l => l.PurchaseInvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Item).WithMany(i => i.PurchaseLines)
            .HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);

        // Nullable FK — set on confirmation for batch-tracked items only
        builder.HasOne(l => l.ItemBatch).WithMany()
            .HasForeignKey(l => l.ItemBatchId).OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("sales_invoices");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.InvoiceNumber).IsUnique();
        builder.Property(s => s.Status).HasConversion<string>();
        builder.Property(s => s.PaymentMethod).HasConversion<string>();
        builder.Property(s => s.SubTotal).HasPrecision(18, 4);
        builder.Property(s => s.TaxAmount).HasPrecision(18, 4);
        builder.Property(s => s.DiscountAmount).HasPrecision(18, 4);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 4);
        builder.Property(s => s.PaidAmount).HasPrecision(18, 4);

        // Dashboard: filter by status + date range; also used for top-items aggregation
        builder.HasIndex(s => new { s.Status, s.SaleDate })
            .HasDatabaseName("IX_sales_invoices_Status_SaleDate");

        builder.HasOne(s => s.Customer).WithMany(c => c.SalesInvoices)
            .HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(s => s.CreatedBy).WithMany()
            .HasForeignKey(s => s.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalesInvoiceLineConfiguration : IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> builder)
    {
        builder.ToTable("sales_invoice_lines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);
        builder.Property(l => l.TaxPercent).HasPrecision(5, 2);

        builder.HasOne(l => l.SalesInvoice).WithMany(s => s.Lines)
            .HasForeignKey(l => l.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Item).WithMany(i => i.SalesLines)
            .HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.ItemBatch).WithMany(b => b.SalesLines)
            .HasForeignKey(l => l.ItemBatchId).OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}

/// <summary>
/// Allocation table: one row per batch consumed per sales invoice line.
/// Supports multi-batch FEFO splits and full traceability.
/// </summary>
public class SalesInvoiceLineAllocationConfiguration
    : IEntityTypeConfiguration<SalesInvoiceLineAllocation>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLineAllocation> builder)
    {
        builder.ToTable("sales_invoice_line_allocations");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(a => a.UnitCost).HasPrecision(18, 4).IsRequired();

        // FK → SalesInvoiceLine (cascade: deleting a line removes its allocations)
        builder.HasOne(a => a.SalesInvoiceLine)
            .WithMany(l => l.Allocations)
            .HasForeignKey(a => a.SalesInvoiceLineId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → ItemBatch (nullable: non-batch items have null)
        builder.HasOne(a => a.ItemBatch)
            .WithMany()
            .HasForeignKey(a => a.ItemBatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Composite index for batch-level queries (e.g. "which sales consumed batch X?")
        builder.HasIndex(a => new { a.ItemBatchId, a.SalesInvoiceLineId });
    }
}

