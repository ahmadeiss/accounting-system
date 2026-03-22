using Accounting.Application.Auth;
using Accounting.Application.Import.DTOs;
using Accounting.Application.Import.Services;
using Accounting.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.API.Controllers;

/// <summary>
/// Excel import endpoints. Requires imports.run permission.
/// All endpoints accept multipart/form-data with an .xlsx file.
/// Pass ?dryRun=true to validate without persisting.
/// </summary>
[Route("api/v1/imports")]
public class ImportsController : BaseController
{
    private readonly IImportService _importService;
    private readonly ICurrentUserService _currentUser;

    public ImportsController(IImportService importService, ICurrentUserService currentUser)
    {
        _importService = importService;
        _currentUser   = currentUser;
    }

    // ─── GET /api/v1/imports/{id} ─────────────────────────────────────────────

    /// <summary>Returns the result of a previously executed import job.</summary>
    [HttpGet("{id:guid}", Name = "GetImportJobById")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _importService.GetJobResultAsync(id, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    // ─── POST /api/v1/imports/items ───────────────────────────────────────────

    /// <summary>
    /// Imports item master data from an Excel file.
    /// Columns: Name, SKU, Barcode, CategoryName, UnitName, CostPrice, SalePrice,
    ///          ReorderLevel, TrackBatch, TrackExpiry, MinExpiryDaysBeforeSale.
    /// </summary>
    [HttpPost("items")]
    [Authorize(Policy = PermissionNames.ImportsRun)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportItems(
        IFormFile file,
        [FromQuery] bool dryRun = false,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("An Excel file (.xlsx) is required.");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx files are supported.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var request = new ImportItemsRequest(
            FileBytes: ms.ToArray(),
            OriginalFileName: file.FileName,
            CreatedById: _currentUser.UserId,
            DryRun: dryRun);

        var result = await _importService.ImportItemsAsync(request, ct);

        return result.HasErrors
            ? UnprocessableEntity(result)
            : Ok(result);
    }

    // ─── POST /api/v1/imports/opening-stock ───────────────────────────────────

    /// <summary>
    /// Imports opening stock from an Excel file.
    /// Columns: SKU, Quantity, CostPerUnit, BatchNumber, ExpiryDate, ProductionDate.
    /// All stock mutations flow through IStockService (StockMovementType.Opening).
    /// </summary>
    [HttpPost("opening-stock")]
    [Authorize(Policy = PermissionNames.ImportsRun)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportOpeningStock(
        IFormFile file,
        [FromQuery] Guid warehouseId,
        [FromQuery] bool dryRun = false,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("An Excel file (.xlsx) is required.");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx files are supported.");

        if (warehouseId == Guid.Empty)
            return BadRequest("warehouseId is required.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var request = new ImportOpeningStockRequest(
            FileBytes: ms.ToArray(),
            OriginalFileName: file.FileName,
            WarehouseId: warehouseId,
            CreatedById: _currentUser.UserId,
            DryRun: dryRun);

        var result = await _importService.ImportOpeningStockAsync(request, ct);

        return result.HasErrors
            ? UnprocessableEntity(result)
            : Ok(result);
    }
}

