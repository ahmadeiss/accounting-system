using System.Data;
using Accounting.Core.Entities;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Accounting.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AccountingDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AccountingDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        Roles = new Repository<Role>(context);
        Permissions = new Repository<Permission>(context);
        Branches = new Repository<Branch>(context);
        Warehouses = new Repository<Warehouse>(context);
        Categories = new Repository<Category>(context);
        Units = new Repository<Unit>(context);
        Items = new Repository<Item>(context);
        ItemBatches = new Repository<ItemBatch>(context);
        Suppliers = new Repository<Supplier>(context);
        Customers = new Repository<Customer>(context);
        PurchaseInvoices = new Repository<PurchaseInvoice>(context);
        PurchaseInvoiceLines = new Repository<PurchaseInvoiceLine>(context);
        SalesInvoices = new Repository<SalesInvoice>(context);
        SalesInvoiceLines = new Repository<SalesInvoiceLine>(context);
        SalesInvoiceLineAllocations = new Repository<SalesInvoiceLineAllocation>(context);
        StockMovements = new Repository<StockMovement>(context);
        StockBalances = new Repository<StockBalance>(context);
        Alerts = new Repository<Alert>(context);
        ImportJobs = new Repository<ImportJob>(context);
        ImportJobRows = new Repository<ImportJobRow>(context);
    }

    public IRepository<User> Users { get; }
    public IRepository<Role> Roles { get; }
    public IRepository<Permission> Permissions { get; }
    public IRepository<Branch> Branches { get; }
    public IRepository<Warehouse> Warehouses { get; }
    public IRepository<Category> Categories { get; }
    public IRepository<Unit> Units { get; }
    public IRepository<Item> Items { get; }
    public IRepository<ItemBatch> ItemBatches { get; }
    public IRepository<Supplier> Suppliers { get; }
    public IRepository<Customer> Customers { get; }
    public IRepository<PurchaseInvoice> PurchaseInvoices { get; }
    public IRepository<PurchaseInvoiceLine> PurchaseInvoiceLines { get; }
    public IRepository<SalesInvoice> SalesInvoices { get; }
    public IRepository<SalesInvoiceLine> SalesInvoiceLines { get; }
    public IRepository<SalesInvoiceLineAllocation> SalesInvoiceLineAllocations { get; }
    public IRepository<StockMovement> StockMovements { get; }
    public IRepository<StockBalance> StockBalances { get; }
    public IRepository<Alert> Alerts { get; }
    public IRepository<ImportJob> ImportJobs { get; }
    public IRepository<ImportJobRow> ImportJobRows { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

