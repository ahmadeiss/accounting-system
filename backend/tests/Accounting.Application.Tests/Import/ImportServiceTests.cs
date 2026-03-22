using Accounting.Application.Import.DTOs;
using Accounting.Application.Import.Services;
using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Accounting.Application.Tests.Import;

/// <summary>
/// Integration-style tests for ImportService using SQLite in-memory.
///
/// Scenarios:
///   1. Item import dry-run — all valid rows → Completed, no DB writes.
///   2. Item import dry-run — one invalid row → PartialSuccess, no DB writes.
///   3. Item import commit — all valid → items persisted, ImportJob created.
///   4. Item import commit — duplicate SKU in file → second row fails.
///   5. Item import commit — SKU already in DB → row fails.
///   6. Opening stock import dry-run — valid rows → Completed, no stock written.
///   7. Opening stock import commit — valid non-batch item → StockMovement created.
///   8. Opening stock import commit — unknown SKU → row fails.
/// </summary>
public class ImportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AccountingDbContext _context;
    private readonly IImportService _service;

    private static readonly Guid _actorId = Guid.NewGuid();
    private static readonly Guid _branchId = Guid.NewGuid();
    private static readonly Guid _warehouseId = Guid.NewGuid();
    private static readonly Guid _categoryId = Guid.NewGuid();
    private static readonly Guid _unitId = Guid.NewGuid();

    static ImportServiceTests()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public ImportServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AccountingDbContext(options);
        _context.Database.EnsureCreated();

        SeedDatabase();

        var stock = new StockService(_context);
        var audit = new AuditService(_context);
        var itemProcessor = new ItemImportProcessor(_context);
        var stockProcessor = new OpeningStockImportProcessor(_context, stock);

        _service = new ImportService(_context, audit, itemProcessor, stockProcessor);
    }

    // ─── 1. Item dry-run all valid ────────────────────────────────────────────

    [Fact]
    public async Task ImportItemsAsync_DryRun_AllValid_ReturnsCompleted_NothingPersisted()
    {
        var bytes = BuildItemExcel(new[]
        {
            ("Widget A", "SKU-W-001", "General", "Piece", "5.00", "10.00", "false", "false")
        });

        var result = await _service.ImportItemsAsync(
            new ImportItemsRequest(bytes, "items.xlsx", _actorId, DryRun: true));

        result.IsDryRun.Should().BeTrue();
        result.Status.Should().Be(ImportJobStatus.Completed);
        result.SuccessRows.Should().Be(1);
        result.FailedRows.Should().Be(0);
        result.JobId.Should().BeNull();

        // Nothing persisted
        _context.Items.Count().Should().Be(1); // only the seeded item
        _context.ImportJobs.Count().Should().Be(0);
    }

    // ─── 2. Item dry-run with invalid row ─────────────────────────────────────

    [Fact]
    public async Task ImportItemsAsync_DryRun_InvalidRow_ReturnsPartialSuccess_NothingPersisted()
    {
        var bytes = BuildItemExcel(new[]
        {
            ("Widget B", "SKU-W-002", "General", "Piece", "5.00", "10.00", "false", "false"),
            ("",         "SKU-W-003", "General", "Piece", "5.00", "10.00", "false", "false") // missing Name
        });

        var result = await _service.ImportItemsAsync(
            new ImportItemsRequest(bytes, "items.xlsx", _actorId, DryRun: true));

        result.IsDryRun.Should().BeTrue();
        result.Status.Should().Be(ImportJobStatus.PartialSuccess);
        result.SuccessRows.Should().Be(1);
        result.FailedRows.Should().Be(1);
        result.Rows.First(r => r.RowNumber == 3).ErrorMessage.Should().Contain("Name");
        _context.ImportJobs.Count().Should().Be(0);
    }

    // ─── 3. Item commit all valid ─────────────────────────────────────────────

    [Fact]
    public async Task ImportItemsAsync_Commit_AllValid_ItemsAndJobPersisted()
    {
        var bytes = BuildItemExcel(new[]
        {
            ("Widget C", "SKU-W-004", "General", "Piece", "5.00", "10.00", "false", "false")
        });

        var result = await _service.ImportItemsAsync(
            new ImportItemsRequest(bytes, "items.xlsx", _actorId, DryRun: false));

        result.Status.Should().Be(ImportJobStatus.Completed);
        result.SuccessRows.Should().Be(1);
        result.JobId.Should().NotBeNull();

        _context.Items.Any(i => i.SKU == "SKU-W-004").Should().BeTrue();
        _context.ImportJobs.Any(j => j.Id == result.JobId).Should().BeTrue();
    }

    // ─── 4. Item commit — duplicate SKU in file ───────────────────────────────

    [Fact]
    public async Task ImportItemsAsync_Commit_DuplicateSkuInFile_SecondRowFails()
    {
        var bytes = BuildItemExcel(new[]
        {
            ("Widget D", "SKU-DUP-001", "General", "Piece", "5.00", "10.00", "false", "false"),
            ("Widget E", "SKU-DUP-001", "General", "Piece", "5.00", "10.00", "false", "false")
        });

        var result = await _service.ImportItemsAsync(
            new ImportItemsRequest(bytes, "items.xlsx", _actorId, DryRun: false));

        result.Status.Should().Be(ImportJobStatus.PartialSuccess);
        result.SuccessRows.Should().Be(1);
        result.FailedRows.Should().Be(1);
        result.Rows.First(r => r.Status == ImportRowStatus.Failed).ErrorMessage
            .Should().Contain("Duplicate SKU");
    }

    // ─── 5. Item commit — SKU already in DB ──────────────────────────────────

    [Fact]
    public async Task ImportItemsAsync_Commit_SkuAlreadyInDb_RowFails()
    {
        // "SKU-EXISTING" is seeded in SeedDatabase()
        var bytes = BuildItemExcel(new[]
        {
            ("Existing Item", "SKU-EXISTING", "General", "Piece", "5.00", "10.00", "false", "false")
        });

        var result = await _service.ImportItemsAsync(
            new ImportItemsRequest(bytes, "items.xlsx", _actorId, DryRun: false));

        result.Status.Should().Be(ImportJobStatus.Failed);
        result.FailedRows.Should().Be(1);
        result.Rows[0].ErrorMessage.Should().Contain("already exists");
    }

    // ─── 6. Opening stock dry-run valid ──────────────────────────────────────

    [Fact]
    public async Task ImportOpeningStockAsync_DryRun_Valid_ReturnsCompleted_NoStockWritten()
    {
        var bytes = BuildStockExcel(new[]
        {
            ("SKU-EXISTING", "50", "5.00", "", "")
        });

        var result = await _service.ImportOpeningStockAsync(
            new ImportOpeningStockRequest(bytes, "stock.xlsx", _warehouseId, _actorId, DryRun: true));

        result.IsDryRun.Should().BeTrue();
        result.Status.Should().Be(ImportJobStatus.Completed);
        result.SuccessRows.Should().Be(1);
        _context.StockMovements.Count().Should().Be(0);
        _context.ImportJobs.Count().Should().Be(0);
    }

    // ─── 7. Opening stock commit — valid non-batch item ───────────────────────

    [Fact]
    public async Task ImportOpeningStockAsync_Commit_ValidNonBatchItem_StockMovementCreated()
    {
        var bytes = BuildStockExcel(new[]
        {
            ("SKU-EXISTING", "100", "4.50", "", "")
        });

        var result = await _service.ImportOpeningStockAsync(
            new ImportOpeningStockRequest(bytes, "stock.xlsx", _warehouseId, _actorId, DryRun: false));

        result.Status.Should().Be(ImportJobStatus.Completed);
        result.SuccessRows.Should().Be(1);

        var movement = _context.StockMovements.FirstOrDefault();
        movement.Should().NotBeNull();
        movement!.MovementType.Should().Be(StockMovementType.Opening);
        movement.Quantity.Should().Be(100);
    }

    // ─── 8. Opening stock commit — unknown SKU ────────────────────────────────

    [Fact]
    public async Task ImportOpeningStockAsync_Commit_UnknownSku_RowFails()
    {
        var bytes = BuildStockExcel(new[]
        {
            ("SKU-DOES-NOT-EXIST", "10", "5.00", "", "")
        });

        var result = await _service.ImportOpeningStockAsync(
            new ImportOpeningStockRequest(bytes, "stock.xlsx", _warehouseId, _actorId, DryRun: false));

        result.Status.Should().Be(ImportJobStatus.Failed);
        result.FailedRows.Should().Be(1);
        result.Rows[0].ErrorMessage.Should().Contain("not found");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Builds an in-memory .xlsx for item import.</summary>
    private static byte[] BuildItemExcel(
        IEnumerable<(string Name, string Sku, string Category, string Unit,
                     string Cost, string Sale, string TrackBatch, string TrackExpiry)> rows)
    {
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Items");
        ws.Cells[1, 1].Value = "Name";
        ws.Cells[1, 2].Value = "SKU";
        ws.Cells[1, 3].Value = "CategoryName";
        ws.Cells[1, 4].Value = "UnitName";
        ws.Cells[1, 5].Value = "CostPrice";
        ws.Cells[1, 6].Value = "SalePrice";
        ws.Cells[1, 7].Value = "TrackBatch";
        ws.Cells[1, 8].Value = "TrackExpiry";

        int r = 2;
        foreach (var row in rows)
        {
            ws.Cells[r, 1].Value = row.Name;
            ws.Cells[r, 2].Value = row.Sku;
            ws.Cells[r, 3].Value = row.Category;
            ws.Cells[r, 4].Value = row.Unit;
            ws.Cells[r, 5].Value = row.Cost;
            ws.Cells[r, 6].Value = row.Sale;
            ws.Cells[r, 7].Value = row.TrackBatch;
            ws.Cells[r, 8].Value = row.TrackExpiry;
            r++;
        }
        return pkg.GetAsByteArray();
    }

    /// <summary>Builds an in-memory .xlsx for opening stock import.</summary>
    private static byte[] BuildStockExcel(
        IEnumerable<(string Sku, string Qty, string Cost, string BatchNumber, string ExpiryDate)> rows)
    {
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Stock");
        ws.Cells[1, 1].Value = "SKU";
        ws.Cells[1, 2].Value = "Quantity";
        ws.Cells[1, 3].Value = "CostPerUnit";
        ws.Cells[1, 4].Value = "BatchNumber";
        ws.Cells[1, 5].Value = "ExpiryDate";

        int r = 2;
        foreach (var row in rows)
        {
            ws.Cells[r, 1].Value = row.Sku;
            ws.Cells[r, 2].Value = row.Qty;
            ws.Cells[r, 3].Value = row.Cost;
            ws.Cells[r, 4].Value = row.BatchNumber;
            ws.Cells[r, 5].Value = row.ExpiryDate;
            r++;
        }
        return pkg.GetAsByteArray();
    }

    private void SeedDatabase()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        _context.Roles.Add(role);
        _context.SaveChanges();

        _context.Users.Add(new User
        {
            Id = _actorId,
            Username = "importer",
            FirstName = "Import",
            LastName = "User",
            Email = "import@test.com",
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

        // Pre-existing item for duplicate-SKU and opening-stock tests
        _context.Items.Add(new Item
        {
            Name = "Existing Item",
            SKU = "SKU-EXISTING",
            CategoryId = _categoryId,
            UnitId = _unitId,
            CostPrice = 5,
            SalePrice = 10,
            TrackBatch = false,
            TrackExpiry = false
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

