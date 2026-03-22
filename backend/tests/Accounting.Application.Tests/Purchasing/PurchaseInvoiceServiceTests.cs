using Accounting.Application.Common.Mappings;
using Accounting.Application.Purchasing.DTOs;
using Accounting.Application.Purchasing.Services;
using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Exceptions;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Repositories;
using Accounting.Infrastructure.Services;
using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Tests.Purchasing;

/// <summary>
/// Integration-style tests for PurchaseInvoiceService using SQLite in-memory.
/// Each test gets a fresh database via a unique connection string.
/// </summary>
public class PurchaseInvoiceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AccountingDbContext _context;
    private readonly IPurchaseInvoiceService _service;

    // ─── Seed IDs ────────────────────────────────────────────────────────────
    private static readonly Guid _actorId = Guid.NewGuid();
    private static readonly Guid _supplierId = Guid.NewGuid();
    private static readonly Guid _branchId = Guid.NewGuid();
    private static readonly Guid _warehouseId = Guid.NewGuid();
    private static readonly Guid _categoryId = Guid.NewGuid();
    private static readonly Guid _unitId = Guid.NewGuid();
    private static readonly Guid _batchItemId = Guid.NewGuid();
    private static readonly Guid _nonBatchItemId = Guid.NewGuid();
    private static readonly Guid _expiryItemId = Guid.NewGuid();

    public PurchaseInvoiceServiceTests()
    {
        // SQLite in-memory with a named connection so EF can share it
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AccountingDbContext(options);
        _context.Database.EnsureCreated();

        SeedDatabase();

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>())
            .CreateMapper();

        var uow = new UnitOfWork(_context);
        var stock = new StockService(_context);
        var audit = new AuditService(_context);

        _service = new PurchaseInvoiceService(_context, stock, audit, uow, mapper);
    }

    // ─── Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDraftInvoiceId()
    {
        var request = BuildRequest(new[]
        {
            new CreatePurchaseInvoiceLineRequest(
                _nonBatchItemId, 10, 5.00m, 0, 0, null, null, null, null)
        });

        var id = await _service.CreateAsync(request, _actorId);

        id.Should().NotBeEmpty();
        var invoice = await _context.PurchaseInvoices.FindAsync(id);
        invoice.Should().NotBeNull();
        invoice!.Status.Should().Be(PurchaseInvoiceStatus.Draft);
        invoice.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task ConfirmAsync_NonBatchItem_CreatesStockMovementAndBalance()
    {
        var request = BuildRequest(new[]
        {
            new CreatePurchaseInvoiceLineRequest(
                _nonBatchItemId, 20, 3.50m, 0, 0, null, null, null, null)
        });
        var id = await _service.CreateAsync(request, _actorId);

        await _service.ConfirmAsync(id, _actorId);

        var invoice = await _context.PurchaseInvoices.FindAsync(id);
        invoice!.Status.Should().Be(PurchaseInvoiceStatus.Confirmed);

        var movement = _context.StockMovements
            .FirstOrDefault(m => m.ItemId == _nonBatchItemId && m.ReferenceId == id);
        movement.Should().NotBeNull();
        movement!.Quantity.Should().Be(20);
        movement.MovementType.Should().Be(StockMovementType.Purchase);

        var balance = _context.StockBalances
            .FirstOrDefault(b => b.ItemId == _nonBatchItemId && b.WarehouseId == _warehouseId);
        balance.Should().NotBeNull();
        balance!.QuantityOnHand.Should().Be(20);
    }

    [Fact]
    public async Task ConfirmAsync_BatchTrackedItem_CreatesBatchAndLinksToLine()
    {
        var request = BuildRequest(new[]
        {
            new CreatePurchaseInvoiceLineRequest(
                _batchItemId, 50, 2.00m, 0, 0,
                "BATCH-001", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(180)), null)
        });
        var id = await _service.CreateAsync(request, _actorId);

        await _service.ConfirmAsync(id, _actorId);

        var line = _context.PurchaseInvoiceLines
            .FirstOrDefault(l => l.PurchaseInvoiceId == id);
        line.Should().NotBeNull();
        line!.ItemBatchId.Should().NotBeNull();

        var batch = await _context.ItemBatches.FindAsync(line.ItemBatchId);
        batch.Should().NotBeNull();
        batch!.BatchNumber.Should().Be("BATCH-001");
        batch.ReceivedQuantity.Should().Be(50);
        batch.AvailableQuantity.Should().Be(50);
    }

    [Fact]
    public async Task ConfirmAsync_AlreadyConfirmed_ThrowsInvalidInvoiceStatusException()
    {
        var request = BuildRequest(new[]
        {
            new CreatePurchaseInvoiceLineRequest(
                _nonBatchItemId, 5, 1.00m, 0, 0, null, null, null, null)
        });
        var id = await _service.CreateAsync(request, _actorId);
        await _service.ConfirmAsync(id, _actorId);

        // Second confirm must fail
        var act = async () => await _service.ConfirmAsync(id, _actorId);

        await act.Should().ThrowAsync<InvalidInvoiceStatusException>()
            .WithMessage("*Confirmed*Draft*");
    }

    [Fact]
    public async Task ConfirmAsync_BatchItemMissingBatchNumber_ThrowsMissingBatchDataException()
    {
        var request = BuildRequest(new[]
        {
            new CreatePurchaseInvoiceLineRequest(
                _batchItemId, 10, 1.00m, 0, 0,
                null,   // ← missing BatchNumber
                null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)), null)
        });
        var id = await _service.CreateAsync(request, _actorId);

        var act = async () => await _service.ConfirmAsync(id, _actorId);

        await act.Should().ThrowAsync<MissingBatchDataException>()
            .WithMessage("*BatchNumber*");
    }

    [Fact]
    public async Task ConfirmAsync_ExpiryItemMissingExpiryDate_ThrowsMissingBatchDataException()
    {
        var request = BuildRequest(new[]
        {
            new CreatePurchaseInvoiceLineRequest(
                _expiryItemId, 10, 1.00m, 0, 0,
                "BATCH-EXP",
                null,
                null,   // ← missing ExpiryDate
                null)
        });
        var id = await _service.CreateAsync(request, _actorId);

        var act = async () => await _service.ConfirmAsync(id, _actorId);

        await act.Should().ThrowAsync<MissingBatchDataException>()
            .WithMessage("*ExpiryDate*");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private CreatePurchaseInvoiceRequest BuildRequest(
        IReadOnlyList<CreatePurchaseInvoiceLineRequest> lines)
        => new(
            SupplierId: _supplierId,
            BranchId: _branchId,
            WarehouseId: _warehouseId,
            InvoiceDate: DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate: null,
            Notes: null,
            Lines: lines);

    private void SeedDatabase()
    {
        // Role is required by User FK
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        _context.Roles.Add(role);
        _context.SaveChanges();

        var actor = new User
        {
            Id = _actorId,
            Username = "testactor",
            FirstName = "Test",
            LastName = "Actor",
            Email = "actor@test.com",
            PasswordHash = "x",
            RoleId = role.Id
        };

        var supplier = new Supplier
        {
            Id = _supplierId,
            Name = "Test Supplier",
            Code = "SUP-001"
        };

        var branch = new Branch
        {
            Id = _branchId,
            Name = "Main Branch",
            Code = "BR-001"
        };

        var warehouse = new Warehouse
        {
            Id = _warehouseId,
            Name = "Main Warehouse",
            Code = "WH-001",
            BranchId = _branchId
        };

        var category = new Category { Id = _categoryId, Name = "General" };
        var unit = new Unit { Id = _unitId, Name = "Piece", Abbreviation = "pcs" };

        var nonBatchItem = new Item
        {
            Id = _nonBatchItemId,
            Name = "Non-Batch Item",
            SKU = "SKU-NB-001",
            CategoryId = _categoryId,
            UnitId = _unitId,
            TrackBatch = false,
            TrackExpiry = false,
            SalePrice = 10,
            CostPrice = 5
        };

        var batchItem = new Item
        {
            Id = _batchItemId,
            Name = "Batch Item",
            SKU = "SKU-B-001",
            CategoryId = _categoryId,
            UnitId = _unitId,
            TrackBatch = true,
            TrackExpiry = false,
            SalePrice = 10,
            CostPrice = 5
        };

        var expiryItem = new Item
        {
            Id = _expiryItemId,
            Name = "Expiry Item",
            SKU = "SKU-EX-001",
            CategoryId = _categoryId,
            UnitId = _unitId,
            TrackBatch = true,
            TrackExpiry = true,
            SalePrice = 10,
            CostPrice = 5
        };

        _context.Users.Add(actor);
        _context.Suppliers.Add(supplier);
        _context.Branches.Add(branch);
        _context.Warehouses.Add(warehouse);
        _context.Categories.Add(category);
        _context.Units.Add(unit);
        _context.Items.AddRange(nonBatchItem, batchItem, expiryItem);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

