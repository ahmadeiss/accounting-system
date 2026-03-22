using Accounting.Core.Entities;

namespace Accounting.Core.Interfaces;

/// <summary>
/// Unit of Work pattern: groups multiple repository operations into a single transaction.
/// All service methods that write data must call SaveChangesAsync at the end.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Role> Roles { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<Branch> Branches { get; }
    IRepository<Warehouse> Warehouses { get; }
    IRepository<Category> Categories { get; }
    IRepository<Unit> Units { get; }
    IRepository<Item> Items { get; }
    IRepository<ItemBatch> ItemBatches { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<Customer> Customers { get; }
    IRepository<PurchaseInvoice> PurchaseInvoices { get; }
    IRepository<PurchaseInvoiceLine> PurchaseInvoiceLines { get; }
    IRepository<SalesInvoice> SalesInvoices { get; }
    IRepository<SalesInvoiceLine> SalesInvoiceLines { get; }
    IRepository<SalesInvoiceLineAllocation> SalesInvoiceLineAllocations { get; }
    IRepository<StockMovement> StockMovements { get; }
    IRepository<StockBalance> StockBalances { get; }
    IRepository<Alert> Alerts { get; }
    IRepository<ImportJob> ImportJobs { get; }
    IRepository<ImportJobRow> ImportJobRows { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

