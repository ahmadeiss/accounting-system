using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Tests.Dashboard;

/// <summary>
/// Integration-style tests for DashboardService using SQLite in-memory.
///
/// Scenarios:
///   1. GetSummaryAsync — returns correct sales, purchase, inventory, and alert counts.
///   2. GetSalesSummaryAsync — only Completed invoices count; Draft invoices are excluded.
///   3. GetPurchaseSummaryAsync — only Confirmed invoices count; Draft invoices are excluded.
///   4. GetInventorySummaryAsync — correctly counts low-stock and out-of-stock items.
///   5. GetSalesTrendAsync — groups by day, returns correct daily revenue.
///   6. GetTopSellingItemsAsync — ranks items by quantity sold, respects top N limit.
///   7. GetExpiryRiskAsync — returns expired and near-expiry batches, excludes safe batches.
///   8. Warehouse filter — all methods respect optional warehouseId filter.
/// </summary>
public class DashboardServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AccountingDbContext _db;
    private readonly DashboardService _svc;

    // Shared seed IDs
    private static readonly Guid _branchId    = Guid.NewGuid();
    private static readonly Guid _warehouseId = Guid.NewGuid();
    private static readonly Guid _categoryId  = Guid.NewGuid();
    private static readonly Guid _unitId      = Guid.NewGuid();
    private static readonly Guid _roleId      = Guid.NewGuid();
    private static readonly Guid _userId      = Guid.NewGuid();
    private static readonly Guid _itemAId     = Guid.NewGuid();
    private static readonly Guid _itemBId     = Guid.NewGuid();

    public DashboardServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AccountingDbContext(options);
        _db.Database.EnsureCreated();

        SeedDatabase();

        _svc = new DashboardService(_db);
    }

    private void SeedDatabase()
    {
        var branch = new Branch
        {
            Id = _branchId, Name = "Main Branch", Code = "MB",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var warehouse = new Warehouse
        {
            Id = _warehouseId, Name = "Main Warehouse", Code = "MW",
            BranchId = _branchId, IsDefault = true, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var category = new Category
        {
            Id = _categoryId, Name = "Food",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var unit = new Unit
        {
            Id = _unitId, Name = "Piece", Abbreviation = "pcs",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var role = new Role
        {
            Id = _roleId, Name = "Admin", Description = "Admin role",
            IsSystemRole = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var user = new User
        {
            Id = _userId, Username = "admin", PasswordHash = "x",
            FirstName = "Admin", LastName = "User", IsActive = true,
            RoleId = _roleId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var itemA = new Item
        {
            Id = _itemAId, Name = "Apple Juice", SKU = "AJ-001",
            CategoryId = _categoryId, UnitId = _unitId,
            CostPrice = 2m, SalePrice = 3m, ReorderLevel = 10m,
            TrackBatch = true, TrackExpiry = true, MinExpiryDaysBeforeSale = 3,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var itemB = new Item
        {
            Id = _itemBId, Name = "Milk", SKU = "ML-001",
            CategoryId = _categoryId, UnitId = _unitId,
            CostPrice = 1m, SalePrice = 1.5m, ReorderLevel = 5m,
            TrackBatch = false, TrackExpiry = false, MinExpiryDaysBeforeSale = 0,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };

        _db.Branches.Add(branch);
        _db.Warehouses.Add(warehouse);
        _db.Categories.Add(category);
        _db.Units.Add(unit);
        _db.Roles.Add(role);
        _db.SaveChanges(); // role must exist before user FK

        _db.Users.Add(user);
        _db.Items.AddRange(itemA, itemB);
        _db.SaveChanges();

        SeedStockBalances();
        SeedInvoices();
        SeedBatches();
        SeedAlerts();
    }

    private void SeedStockBalances()
    {
        // ItemA: 8 units (below reorder level of 10 → low stock)
        // ItemB: 0 units (out of stock)
        _db.StockBalances.AddRange(
            new StockBalance
            {
                Id = Guid.NewGuid(), ItemId = _itemAId, WarehouseId = _warehouseId,
                QuantityOnHand = 8m, LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            },
            new StockBalance
            {
                Id = Guid.NewGuid(), ItemId = _itemBId, WarehouseId = _warehouseId,
                QuantityOnHand = 0m, LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedInvoices()
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        // Completed sales invoice (today): 30 total, 3 tax, 0 discount
        var saleCompleted = new SalesInvoice
        {
            Id = Guid.NewGuid(), InvoiceNumber = "SAL-001",
            BranchId = _branchId, WarehouseId = _warehouseId,
            SaleDate = today, Status = SalesInvoiceStatus.Completed,
            SubTotal = 27m, TaxAmount = 3m, DiscountAmount = 0m, TotalAmount = 30m, PaidAmount = 30m,
            PaymentMethod = PaymentMethod.Cash, CreatedById = _userId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        // Draft sales invoice — must NOT count in totals
        var saleDraft = new SalesInvoice
        {
            Id = Guid.NewGuid(), InvoiceNumber = "SAL-002",
            BranchId = _branchId, WarehouseId = _warehouseId,
            SaleDate = today, Status = SalesInvoiceStatus.Draft,
            SubTotal = 100m, TaxAmount = 10m, DiscountAmount = 0m, TotalAmount = 110m, PaidAmount = 0m,
            PaymentMethod = PaymentMethod.Cash, CreatedById = _userId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        // Completed sales invoice (yesterday): 20 total
        var saleYesterday = new SalesInvoice
        {
            Id = Guid.NewGuid(), InvoiceNumber = "SAL-003",
            BranchId = _branchId, WarehouseId = _warehouseId,
            SaleDate = yesterday, Status = SalesInvoiceStatus.Completed,
            SubTotal = 18m, TaxAmount = 2m, DiscountAmount = 0m, TotalAmount = 20m, PaidAmount = 20m,
            PaymentMethod = PaymentMethod.Cash, CreatedById = _userId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };

        _db.SalesInvoices.AddRange(saleCompleted, saleDraft, saleYesterday);
        _db.SaveChanges();

        // Sales lines for top-items test: itemA sold 15, itemB sold 5 (in completed invoices)
        _db.SalesInvoiceLines.AddRange(
            new SalesInvoiceLine
            {
                Id = Guid.NewGuid(), SalesInvoiceId = saleCompleted.Id,
                ItemId = _itemAId, Quantity = 10m, UnitPrice = 3m, LineTotal = 30m,
                DiscountPercent = 0m, TaxPercent = 0m,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            },
            new SalesInvoiceLine
            {
                Id = Guid.NewGuid(), SalesInvoiceId = saleYesterday.Id,
                ItemId = _itemAId, Quantity = 5m, UnitPrice = 3m, LineTotal = 15m,
                DiscountPercent = 0m, TaxPercent = 0m,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            },
            new SalesInvoiceLine
            {
                Id = Guid.NewGuid(), SalesInvoiceId = saleYesterday.Id,
                ItemId = _itemBId, Quantity = 5m, UnitPrice = 1.5m, LineTotal = 7.5m,
                DiscountPercent = 0m, TaxPercent = 0m,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            }
        );

        // Confirmed purchase invoice: 50 total, 25 paid → balance due 25
        var purchaseConfirmed = new PurchaseInvoice
        {
            Id = Guid.NewGuid(), InvoiceNumber = "PUR-001",
            SupplierId = SeedSupplier(), BranchId = _branchId, WarehouseId = _warehouseId,
            InvoiceDate = DateOnly.FromDateTime(today), Status = PurchaseInvoiceStatus.Confirmed,
            SubTotal = 45m, TaxAmount = 5m, DiscountAmount = 0m, TotalAmount = 50m, PaidAmount = 25m,
            CreatedById = _userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        // Draft purchase invoice — must NOT count
        var purchaseDraft = new PurchaseInvoice
        {
            Id = Guid.NewGuid(), InvoiceNumber = "PUR-002",
            SupplierId = purchaseConfirmed.SupplierId, BranchId = _branchId, WarehouseId = _warehouseId,
            InvoiceDate = DateOnly.FromDateTime(today), Status = PurchaseInvoiceStatus.Draft,
            SubTotal = 200m, TaxAmount = 20m, DiscountAmount = 0m, TotalAmount = 220m, PaidAmount = 0m,
            CreatedById = _userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };

        _db.PurchaseInvoices.AddRange(purchaseConfirmed, purchaseDraft);
        _db.SaveChanges();
    }

    private Guid SeedSupplier()
    {
        var id = Guid.NewGuid();
        _db.Suppliers.Add(new Supplier
        {
            Id = id, Name = "Test Supplier", Code = "TS-001",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        _db.SaveChanges();
        return id;
    }

    private void SeedBatches()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Batch 1: expires in 10 days → near-expiry risk
        // Batch 2: expired 5 days ago → expired risk
        // Batch 3: expires in 60 days → safe (should NOT appear with withinDays=30)
        _db.ItemBatches.AddRange(
            new ItemBatch
            {
                Id = Guid.NewGuid(), ItemId = _itemAId, WarehouseId = _warehouseId,
                BatchNumber = "BATCH-NEAR", ExpiryDate = today.AddDays(10),
                ReceivedQuantity = 20m, AvailableQuantity = 15m, CostPerUnit = 2m,
                Status = BatchStatus.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            },
            new ItemBatch
            {
                Id = Guid.NewGuid(), ItemId = _itemAId, WarehouseId = _warehouseId,
                BatchNumber = "BATCH-EXPIRED", ExpiryDate = today.AddDays(-5),
                ReceivedQuantity = 10m, AvailableQuantity = 5m, CostPerUnit = 2m,
                Status = BatchStatus.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            },
            new ItemBatch
            {
                Id = Guid.NewGuid(), ItemId = _itemAId, WarehouseId = _warehouseId,
                BatchNumber = "BATCH-SAFE", ExpiryDate = today.AddDays(60),
                ReceivedQuantity = 50m, AvailableQuantity = 50m, CostPerUnit = 2m,
                Status = BatchStatus.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    private void SeedAlerts()
    {
        _db.Alerts.AddRange(
            new Alert
            {
                Id = Guid.NewGuid(), AlertType = AlertType.LowStock,
                Severity = AlertSeverity.Warning, Status = AlertStatus.Active,
                Message = "Low stock", ItemId = _itemAId, WarehouseId = _warehouseId,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            },
            new Alert
            {
                Id = Guid.NewGuid(), AlertType = AlertType.NearExpiry,
                Severity = AlertSeverity.Warning, Status = AlertStatus.Acknowledged,
                Message = "Near expiry",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            },
            new Alert
            {
                Id = Guid.NewGuid(), AlertType = AlertType.ExpiredStock,
                Severity = AlertSeverity.Critical, Status = AlertStatus.Resolved,
                Message = "Expired — already resolved",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }

    // ─── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectCombinedSnapshot()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _svc.GetSummaryAsync(today.AddDays(-7), today);

        result.Sales.InvoiceCount.Should().Be(2);       // 2 Completed invoices
        result.Sales.TotalRevenue.Should().Be(50m);     // 30 + 20
        result.Purchases.InvoiceCount.Should().Be(1);   // 1 Confirmed invoice
        result.Purchases.BalanceDue.Should().Be(25m);   // 50 - 25
        result.Inventory.DistinctItems.Should().Be(2);
        result.Alerts.TotalActive.Should().Be(2);       // Active + Acknowledged; Resolved excluded
    }

    [Fact]
    public async Task GetSalesSummary_ExcludesDraftInvoices()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _svc.GetSummaryAsync(today.AddDays(-7), today);

        // Draft invoice has TotalAmount=110; must not appear
        result.Sales.TotalRevenue.Should().Be(50m);
        result.Sales.InvoiceCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPurchaseSummary_ExcludesDraftInvoices()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _svc.GetSummaryAsync(today.AddDays(-7), today);

        // Draft purchase has TotalAmount=220; must not appear
        result.Purchases.TotalCost.Should().Be(50m);
        result.Purchases.InvoiceCount.Should().Be(1);
    }

    [Fact]
    public async Task GetInventorySummaryAsync_CorrectlyCountsLowStockAndOutOfStock()
    {
        var result = await _svc.GetInventorySummaryAsync();

        result.DistinctItems.Should().Be(2);
        result.LowStockItemCount.Should().Be(1);    // ItemA: 8 ≤ reorder 10
        result.OutOfStockItemCount.Should().Be(1);  // ItemB: 0
    }

    [Fact]
    public async Task GetSalesTrendAsync_GroupsByDayCorrectly()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _svc.GetSalesTrendAsync(today.AddDays(-7), today);

        result.Should().HaveCount(2); // today + yesterday
        result.Sum(d => d.Revenue).Should().Be(50m);

        var todayRow = result.First(d => d.Date == today);
        todayRow.InvoiceCount.Should().Be(1);
        todayRow.Revenue.Should().Be(30m);
    }

    [Fact]
    public async Task GetTopSellingItemsAsync_RanksByQuantitySold()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _svc.GetTopSellingItemsAsync(today.AddDays(-7), today, top: 10);

        result.Should().HaveCount(2);
        result[0].ItemId.Should().Be(_itemAId);   // 15 units sold
        result[0].QuantitySold.Should().Be(15m);
        result[1].ItemId.Should().Be(_itemBId);   // 5 units sold
        result[1].QuantitySold.Should().Be(5m);
    }

    [Fact]
    public async Task GetTopSellingItemsAsync_RespectsTopLimit()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _svc.GetTopSellingItemsAsync(today.AddDays(-7), today, top: 1);

        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(_itemAId);
    }

    [Fact]
    public async Task GetExpiryRiskAsync_ReturnsExpiredAndNearExpiryBatches()
    {
        var result = await _svc.GetExpiryRiskAsync(withinDays: 30);

        // BATCH-NEAR (10 days) + BATCH-EXPIRED (-5 days) should appear
        // BATCH-SAFE (60 days) should NOT appear
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.BatchNumber == "BATCH-NEAR" && !r.IsExpired);
        result.Should().Contain(r => r.BatchNumber == "BATCH-EXPIRED" && r.IsExpired);
        result.Should().NotContain(r => r.BatchNumber == "BATCH-SAFE");
    }

    [Fact]
    public async Task GetExpiryRiskAsync_OrderedByExpiryDateAscending()
    {
        var result = await _svc.GetExpiryRiskAsync(withinDays: 30);

        result.Should().BeInAscendingOrder(r => r.ExpiryDate);
        result[0].BatchNumber.Should().Be("BATCH-EXPIRED"); // most urgent first
    }

    [Fact]
    public async Task GetInventorySummaryAsync_WarehouseFilter_ReturnsOnlyMatchingWarehouse()
    {
        var otherWarehouseId = Guid.NewGuid();
        var result = await _svc.GetInventorySummaryAsync(warehouseId: otherWarehouseId);

        result.DistinctItems.Should().Be(0);
        result.TotalQuantityOnHand.Should().Be(0m);
    }

    [Fact]
    public async Task GetAlertSummary_ExcludesResolvedAlerts()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _svc.GetSummaryAsync(today.AddDays(-7), today);

        // Seeded: 1 Active (LowStock) + 1 Acknowledged (NearExpiry) + 1 Resolved (ExpiredStock)
        result.Alerts.TotalActive.Should().Be(2);
        result.Alerts.LowStock.Should().Be(1);
        result.Alerts.NearExpiry.Should().Be(1);
        result.Alerts.ExpiredStock.Should().Be(0); // Resolved → excluded
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

