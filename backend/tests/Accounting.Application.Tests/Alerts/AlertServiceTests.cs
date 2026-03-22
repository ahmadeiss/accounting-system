using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Accounting.Application.Tests.Alerts;

/// <summary>
/// Integration-style tests for AlertService and AlertScanner using SQLite in-memory.
///
/// Scenarios:
///   1. LowStock alert is created when qty ≤ reorder level.
///   2. NearExpiry alert is created for a batch expiring within the threshold.
///   3. ExpiredStock alert is created for a batch past its expiry date.
///   4. Duplicate suppression — second scan does NOT create a second Active alert.
///   5. Acknowledge transition: Active → Acknowledged; cannot acknowledge Resolved.
///   6. Resolve transition: Active → Resolved; Acknowledged → Resolved.
///   7. Auto-resolve: stale Active alerts are resolved when condition clears.
///   8. Scanner produces correct alert count for a mixed dataset.
/// </summary>
public class AlertServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AccountingDbContext _context;
    private readonly IAlertService _alertService;
    private readonly AlertScanner _scanner;

    // Shared seed IDs
    private static readonly Guid _branchId = Guid.NewGuid();
    private static readonly Guid _warehouseId = Guid.NewGuid();
    private static readonly Guid _categoryId = Guid.NewGuid();
    private static readonly Guid _unitId = Guid.NewGuid();

    public AlertServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AccountingDbContext(options);
        _context.Database.EnsureCreated();

        SeedDatabase();

        _alertService = new AlertService(_context);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Alerts:NearExpiryDays"] = "30"
            })
            .Build();

        _scanner = new AlertScanner(
            _context,
            _alertService,
            config,
            NullLogger<AlertScanner>.Instance);
    }

    // ─── Scenario 1: LowStock alert created ──────────────────────────────────

    [Fact]
    public async Task ScanLowStock_WhenQtyBelowReorderLevel_CreatesAlert()
    {
        // Arrange: item with reorder level 10, balance = 5
        var item = CreateItem("LOW-001", reorderLevel: 10);
        _context.Items.Add(item);
        _context.StockBalances.Add(new StockBalance
        {
            ItemId = item.Id,
            WarehouseId = _warehouseId,
            QuantityOnHand = 5
        });
        await _context.SaveChangesAsync();

        // Act
        await _scanner.ScanAllAsync();

        // Assert
        var alerts = await _context.Alerts.ToListAsync();
        alerts.Should().ContainSingle(a =>
            a.AlertType == AlertType.LowStock &&
            a.Status == AlertStatus.Active &&
            a.ItemId == item.Id);
    }

    // ─── Scenario 2: NearExpiry alert created ────────────────────────────────

    [Fact]
    public async Task ScanNearExpiry_WhenBatchExpiresWithinThreshold_CreatesAlert()
    {
        // Arrange: batch expiring in 15 days
        var item = CreateItem("NEAR-001", trackExpiry: true);
        _context.Items.Add(item);
        var batch = new ItemBatch
        {
            ItemId = item.Id,
            WarehouseId = _warehouseId,
            BatchNumber = "B-NEAR",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            AvailableQuantity = 20,
            Status = BatchStatus.Active
        };
        _context.ItemBatches.Add(batch);
        await _context.SaveChangesAsync();

        // Act
        await _scanner.ScanAllAsync();

        // Assert
        var alerts = await _context.Alerts.ToListAsync();
        alerts.Should().ContainSingle(a =>
            a.AlertType == AlertType.NearExpiry &&
            a.Status == AlertStatus.Active &&
            a.ItemBatchId == batch.Id);
    }

    // ─── Scenario 3: ExpiredStock alert created ───────────────────────────────

    [Fact]
    public async Task ScanExpired_WhenBatchPastExpiry_CreatesAlert()
    {
        // Arrange: batch expired yesterday
        var item = CreateItem("EXP-001", trackExpiry: true);
        _context.Items.Add(item);
        var batch = new ItemBatch
        {
            ItemId = item.Id,
            WarehouseId = _warehouseId,
            BatchNumber = "B-EXP",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            AvailableQuantity = 10,
            Status = BatchStatus.Active
        };
        _context.ItemBatches.Add(batch);
        await _context.SaveChangesAsync();

        // Act
        await _scanner.ScanAllAsync();

        // Assert
        var alerts = await _context.Alerts.ToListAsync();
        alerts.Should().ContainSingle(a =>
            a.AlertType == AlertType.ExpiredStock &&
            a.Status == AlertStatus.Active &&
            a.ItemBatchId == batch.Id);
    }

    // ─── Scenario 4: Duplicate suppression ───────────────────────────────────

    [Fact]
    public async Task ScanLowStock_RunTwice_DoesNotDuplicateAlert()
    {
        // Arrange
        var item = CreateItem("DUP-001", reorderLevel: 10);
        _context.Items.Add(item);
        _context.StockBalances.Add(new StockBalance
        {
            ItemId = item.Id,
            WarehouseId = _warehouseId,
            QuantityOnHand = 3
        });
        await _context.SaveChangesAsync();

        // Act: scan twice
        await _scanner.ScanAllAsync();
        await _scanner.ScanAllAsync();

        // Assert: still only one Active alert
        var alerts = await _context.Alerts
            .Where(a => a.AlertType == AlertType.LowStock && a.ItemId == item.Id)
            .ToListAsync();

        alerts.Should().ContainSingle(a => a.Status == AlertStatus.Active);
    }

    // ─── Scenario 5: Acknowledge transition ──────────────────────────────────

    [Fact]
    public async Task Acknowledge_ActiveAlert_TransitionsToAcknowledged()
    {
        // Arrange: use null itemId — Alert.ItemId is nullable (no FK violation)
        var alert = await _alertService.CreateOrUpdateAlertAsync(
            AlertType.LowStock, AlertSeverity.Warning, "test", null, null, null, null);
        await _context.SaveChangesAsync();

        // Act
        await _alertService.AcknowledgeAsync(alert.Id);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Alerts.FindAsync(alert.Id);
        updated!.Status.Should().Be(AlertStatus.Acknowledged);
    }

    [Fact]
    public async Task Acknowledge_ResolvedAlert_ThrowsInvalidOperation()
    {
        // Arrange
        var alert = await _alertService.CreateOrUpdateAlertAsync(
            AlertType.LowStock, AlertSeverity.Warning, "test", null, null, null, null);
        await _context.SaveChangesAsync();
        await _alertService.ResolveAsync(alert.Id);
        await _context.SaveChangesAsync();

        // Act & Assert
        await _alertService.Invoking(s => s.AcknowledgeAsync(alert.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── Scenario 6: Resolve transition ──────────────────────────────────────

    [Fact]
    public async Task Resolve_AcknowledgedAlert_TransitionsToResolved()
    {
        // Arrange
        var alert = await _alertService.CreateOrUpdateAlertAsync(
            AlertType.LowStock, AlertSeverity.Warning, "test", null, null, null, null);
        await _context.SaveChangesAsync();
        await _alertService.AcknowledgeAsync(alert.Id);
        await _context.SaveChangesAsync();

        // Act
        await _alertService.ResolveAsync(alert.Id);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Alerts.FindAsync(alert.Id);
        updated!.Status.Should().Be(AlertStatus.Resolved);
    }

    // ─── Scenario 7: Auto-resolve stale alerts ────────────────────────────────

    [Fact]
    public async Task AutoResolve_WhenConditionClears_ResolvesActiveAlert()
    {
        // Arrange: create an active alert with no FK references (all nullable)
        var alert = await _alertService.CreateOrUpdateAlertAsync(
            AlertType.LowStock, AlertSeverity.Warning, "low", null, null, null, null);
        await _context.SaveChangesAsync();

        // Act: auto-resolve with an empty "still active" list (condition cleared)
        await _alertService.AutoResolveStaleAsync(AlertType.LowStock, [], CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Alerts.FindAsync(alert.Id);
        updated!.Status.Should().Be(AlertStatus.Resolved);
    }

    [Fact]
    public async Task AutoResolve_AcknowledgedAlert_IsNotAutoResolved()
    {
        // Arrange: acknowledged alert should NOT be auto-resolved
        var alert = await _alertService.CreateOrUpdateAlertAsync(
            AlertType.LowStock, AlertSeverity.Warning, "low", null, null, null, null);
        await _context.SaveChangesAsync();
        await _alertService.AcknowledgeAsync(alert.Id);
        await _context.SaveChangesAsync();

        // Act
        await _alertService.AutoResolveStaleAsync(AlertType.LowStock, [], CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert: still Acknowledged — human must close it
        var updated = await _context.Alerts.FindAsync(alert.Id);
        updated!.Status.Should().Be(AlertStatus.Acknowledged);
    }

    // ─── Scenario 8: Scanner produces correct alert count ────────────────────

    [Fact]
    public async Task Scanner_MixedDataset_ProducesCorrectAlertCount()
    {
        // Arrange: 1 low-stock item + 1 near-expiry batch + 1 expired batch
        var lowItem = CreateItem("MIX-LOW", reorderLevel: 10);
        var nearItem = CreateItem("MIX-NEAR", trackExpiry: true);
        var expItem = CreateItem("MIX-EXP", trackExpiry: true);

        _context.Items.AddRange(lowItem, nearItem, expItem);

        _context.StockBalances.Add(new StockBalance
        {
            ItemId = lowItem.Id, WarehouseId = _warehouseId, QuantityOnHand = 2
        });

        _context.ItemBatches.Add(new ItemBatch
        {
            ItemId = nearItem.Id, WarehouseId = _warehouseId,
            BatchNumber = "MIX-B1",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            AvailableQuantity = 5, Status = BatchStatus.Active
        });

        _context.ItemBatches.Add(new ItemBatch
        {
            ItemId = expItem.Id, WarehouseId = _warehouseId,
            BatchNumber = "MIX-B2",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            AvailableQuantity = 3, Status = BatchStatus.Active
        });

        await _context.SaveChangesAsync();

        // Act
        await _scanner.ScanAllAsync();

        // Assert: exactly 3 active alerts, one of each type
        var alerts = await _context.Alerts.Where(a => a.Status == AlertStatus.Active).ToListAsync();
        alerts.Should().HaveCount(3);
        alerts.Should().Contain(a => a.AlertType == AlertType.LowStock);
        alerts.Should().Contain(a => a.AlertType == AlertType.NearExpiry);
        alerts.Should().Contain(a => a.AlertType == AlertType.ExpiredStock);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Item CreateItem(string sku, decimal reorderLevel = 0, bool trackExpiry = false) => new()
    {
        SKU = sku,
        Name = $"Item {sku}",
        CategoryId = _categoryId,
        UnitId = _unitId,
        ReorderLevel = reorderLevel,
        TrackExpiry = trackExpiry,
        TrackBatch = trackExpiry,
        IsActive = true,
        CostPrice = 10,
        SalePrice = 15
    };

    private void SeedDatabase()
    {
        _context.Branches.Add(new Branch { Id = _branchId, Name = "Test Branch", Code = "BR-TEST" });
        _context.Categories.Add(new Category { Id = _categoryId, Name = "Test Category" });
        _context.Units.Add(new Unit { Id = _unitId, Name = "Unit", Abbreviation = "U" });
        _context.Warehouses.Add(new Warehouse
        {
            Id = _warehouseId, Name = "Main Warehouse", Code = "WH-MAIN", BranchId = _branchId
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

