using Accounting.Application.Common.Mappings;
using Accounting.Application.Sales.DTOs;
using Accounting.Application.Sales.Services;
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

namespace Accounting.Application.Tests.Sales;

/// <summary>
/// Integration-style tests for SalesInvoiceService using SQLite in-memory.
///
/// Scenarios covered:
///   1. CreateAsync → Draft invoice, no stock touched.
///   2. ConfirmAsync non-batch item → StockMovement (negative) + allocation record.
///   3. ConfirmAsync batch item → FEFO single batch consumed, allocation created.
///   4. ConfirmAsync multi-batch FEFO → two batches consumed in expiry order.
///   5. ConfirmAsync expired batch → InsufficientStockException (expired excluded).
///   6. ConfirmAsync oversell → InsufficientStockException.
///   7. ConfirmAsync already-Completed → InvalidInvoiceStatusException.
///   8. ConfirmAsync non-batch insufficient stock → InsufficientStockException.
/// </summary>
public class SalesInvoiceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AccountingDbContext _context;
    private readonly ISalesInvoiceService _service;

    // ─── Seed IDs ────────────────────────────────────────────────────────────
    private static readonly Guid _actorId = Guid.NewGuid();
    private static readonly Guid _branchId = Guid.NewGuid();
    private static readonly Guid _warehouseId = Guid.NewGuid();
    private static readonly Guid _categoryId = Guid.NewGuid();
    private static readonly Guid _unitId = Guid.NewGuid();
    private static readonly Guid _nonBatchItemId = Guid.NewGuid();
    private static readonly Guid _batchItemId = Guid.NewGuid();

    public SalesInvoiceServiceTests()
    {
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
        var batchSelector = new BatchSelectionService(stock);

        _service = new SalesInvoiceService(_context, stock, batchSelector, audit, uow, mapper);
    }

    // ─── 1. Create ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDraftInvoice_NoStockTouched()
    {
        var id = await _service.CreateAsync(BuildRequest(_nonBatchItemId, 5), _actorId);

        id.Should().NotBeEmpty();
        var invoice = await _context.SalesInvoices.FindAsync(id);
        invoice!.Status.Should().Be(SalesInvoiceStatus.Draft);

        // Stock must NOT be touched on create
        _context.StockMovements.Should().BeEmpty();
    }

    // ─── 2. Non-batch confirm ─────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_NonBatchItem_CreatesNegativeMovementAndAllocation()
    {
        // Seed 30 units of non-batch stock
        SeedStockBalance(_nonBatchItemId, 30);

        var id = await _service.CreateAsync(BuildRequest(_nonBatchItemId, 10), _actorId);
        await _service.ConfirmAsync(id, _actorId);

        var invoice = await _context.SalesInvoices.FindAsync(id);
        invoice!.Status.Should().Be(SalesInvoiceStatus.Completed);

        // Negative movement
        var movement = _context.StockMovements
            .FirstOrDefault(m => m.ItemId == _nonBatchItemId && m.ReferenceId == id);
        movement.Should().NotBeNull();
        movement!.Quantity.Should().Be(-10);
        movement.MovementType.Should().Be(StockMovementType.Sale);

        // Balance reduced
        var balance = _context.StockBalances
            .FirstOrDefault(b => b.ItemId == _nonBatchItemId && b.WarehouseId == _warehouseId);
        balance!.QuantityOnHand.Should().Be(20);

        // Allocation record created (null batch)
        var line = _context.SalesInvoiceLines.FirstOrDefault(l => l.SalesInvoiceId == id);
        var alloc = _context.SalesInvoiceLineAllocations
            .FirstOrDefault(a => a.SalesInvoiceLineId == line!.Id);
        alloc.Should().NotBeNull();
        alloc!.ItemBatchId.Should().BeNull();
        alloc.Quantity.Should().Be(10);
    }

    // ─── 3. Single-batch FEFO ────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_BatchItem_SingleBatch_ConsumesCorrectBatch()
    {
        var batchId = SeedBatch(_batchItemId, "BATCH-A", 50,
            expiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)));

        var id = await _service.CreateAsync(BuildRequest(_batchItemId, 20), _actorId);
        await _service.ConfirmAsync(id, _actorId);

        var batch = await _context.ItemBatches.FindAsync(batchId);
        batch!.AvailableQuantity.Should().Be(30);

        var line = _context.SalesInvoiceLines.FirstOrDefault(l => l.SalesInvoiceId == id);
        var alloc = _context.SalesInvoiceLineAllocations
            .FirstOrDefault(a => a.SalesInvoiceLineId == line!.Id);
        alloc!.ItemBatchId.Should().Be(batchId);
        alloc.Quantity.Should().Be(20);
        alloc.ExpiryDateSnapshot.Should().NotBeNull();
    }

    // ─── 4. Multi-batch FEFO ─────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_BatchItem_MultiBatchFEFO_ConsumesEarliestExpiryFirst()
    {
        // Batch A expires sooner → must be consumed first
        var batchA = SeedBatch(_batchItemId, "BATCH-A", 15,
            expiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        var batchB = SeedBatch(_batchItemId, "BATCH-B", 30,
            expiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(120)));

        // Request 20 units → 15 from A, 5 from B
        var id = await _service.CreateAsync(BuildRequest(_batchItemId, 20), _actorId);
        await _service.ConfirmAsync(id, _actorId);

        var line = _context.SalesInvoiceLines.FirstOrDefault(l => l.SalesInvoiceId == id);
        var allocs = _context.SalesInvoiceLineAllocations
            .Where(a => a.SalesInvoiceLineId == line!.Id)
            .OrderBy(a => a.ExpiryDateSnapshot)
            .ToList();

        allocs.Should().HaveCount(2);
        allocs[0].ItemBatchId.Should().Be(batchA);
        allocs[0].Quantity.Should().Be(15);
        allocs[1].ItemBatchId.Should().Be(batchB);
        allocs[1].Quantity.Should().Be(5);

        // Verify batch quantities
        (await _context.ItemBatches.FindAsync(batchA))!.AvailableQuantity.Should().Be(0);
        (await _context.ItemBatches.FindAsync(batchB))!.AvailableQuantity.Should().Be(25);
    }

    // ─── 5. Expired batch excluded ────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_OnlyExpiredBatchAvailable_ThrowsInsufficientStockException()
    {
        // Expired batch — should be excluded by GetFefoBatchesAsync
        SeedBatch(_batchItemId, "BATCH-EXP", 100,
            expiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        var id = await _service.CreateAsync(BuildRequest(_batchItemId, 10), _actorId);

        var act = async () => await _service.ConfirmAsync(id, _actorId);

        await act.Should().ThrowAsync<InsufficientStockException>()
            .WithMessage("*Insufficient stock*");
    }

    // ─── 6. Oversell prevention ───────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_BatchItem_RequestExceedsAvailable_ThrowsInsufficientStockException()
    {
        SeedBatch(_batchItemId, "BATCH-SMALL", 5,
            expiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)));

        // Request 10 but only 5 available
        var id = await _service.CreateAsync(BuildRequest(_batchItemId, 10), _actorId);

        var act = async () => await _service.ConfirmAsync(id, _actorId);

        await act.Should().ThrowAsync<InsufficientStockException>();
    }

    // ─── 7. Re-confirm guard ──────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_AlreadyCompleted_ThrowsInvalidInvoiceStatusException()
    {
        SeedStockBalance(_nonBatchItemId, 50);

        var id = await _service.CreateAsync(BuildRequest(_nonBatchItemId, 5), _actorId);
        await _service.ConfirmAsync(id, _actorId);

        var act = async () => await _service.ConfirmAsync(id, _actorId);

        await act.Should().ThrowAsync<InvalidInvoiceStatusException>()
            .WithMessage("*Completed*Draft*");
    }

    // ─── 8. Non-batch insufficient stock ─────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_NonBatchItem_InsufficientStock_ThrowsInsufficientStockException()
    {
        SeedStockBalance(_nonBatchItemId, 3);

        var id = await _service.CreateAsync(BuildRequest(_nonBatchItemId, 10), _actorId);

        var act = async () => await _service.ConfirmAsync(id, _actorId);

        await act.Should().ThrowAsync<InsufficientStockException>();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private CreateSalesInvoiceRequest BuildRequest(Guid itemId, decimal qty)
        => new(
            BranchId: _branchId,
            WarehouseId: _warehouseId,
            CustomerId: null,
            PaymentMethod: PaymentMethod.Cash,
            PaidAmount: qty * 10,
            Notes: null,
            Lines: new[]
            {
                new CreateSalesInvoiceLineRequest(itemId, qty, 10.00m, 0, 0)
            });

    /// <summary>Seeds a StockBalance (simulates prior purchase receiving).</summary>
    private void SeedStockBalance(Guid itemId, decimal qty)
    {
        _context.StockBalances.Add(new StockBalance
        {
            ItemId = itemId,
            WarehouseId = _warehouseId,
            QuantityOnHand = qty
        });
        _context.SaveChanges();
    }

    /// <summary>
    /// Seeds an ItemBatch with AvailableQuantity and also upserts the StockBalance
    /// so that StockService.RecordMovementAsync does not throw when deducting.
    /// </summary>
    private Guid SeedBatch(Guid itemId, string batchNumber, decimal qty, DateOnly? expiry)
    {
        var batch = new ItemBatch
        {
            ItemId = itemId,
            WarehouseId = _warehouseId,
            BatchNumber = batchNumber,
            ReceivedQuantity = qty,
            AvailableQuantity = qty,
            CostPerUnit = 5.00m,
            ExpiryDate = expiry,
            Status = BatchStatus.Active
        };
        _context.ItemBatches.Add(batch);

        // Keep StockBalance in sync with the batch quantity so the balance check passes.
        var balance = _context.StockBalances
            .FirstOrDefault(b => b.ItemId == itemId && b.WarehouseId == _warehouseId);
        if (balance is null)
        {
            _context.StockBalances.Add(new StockBalance
            {
                ItemId = itemId,
                WarehouseId = _warehouseId,
                QuantityOnHand = qty
            });
        }
        else
        {
            balance.QuantityOnHand += qty;
        }

        _context.SaveChanges();
        return batch.Id;
    }

    private void SeedDatabase()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        _context.Roles.Add(role);
        _context.SaveChanges();

        _context.Users.Add(new User
        {
            Id = _actorId,
            Username = "testactor",
            FirstName = "Test",
            LastName = "Actor",
            Email = "actor@test.com",
            PasswordHash = "x",
            RoleId = role.Id
        });

        _context.Branches.Add(new Branch { Id = _branchId, Name = "Main Branch", Code = "BR-001" });
        _context.Warehouses.Add(new Warehouse
        {
            Id = _warehouseId, Name = "Main Warehouse", Code = "WH-001", BranchId = _branchId
        });
        _context.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _context.Units.Add(new Unit { Id = _unitId, Name = "Piece", Abbreviation = "pcs" });

        _context.Items.Add(new Item
        {
            Id = _nonBatchItemId, Name = "Non-Batch Item", SKU = "SKU-NB-001",
            CategoryId = _categoryId, UnitId = _unitId,
            TrackBatch = false, TrackExpiry = false, SalePrice = 10, CostPrice = 5
        });
        _context.Items.Add(new Item
        {
            Id = _batchItemId, Name = "Batch Item", SKU = "SKU-B-001",
            CategoryId = _categoryId, UnitId = _unitId,
            TrackBatch = true, TrackExpiry = true, SalePrice = 10, CostPrice = 5
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

