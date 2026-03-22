using Accounting.Application.Auth;
using Accounting.Application.Dashboard.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.API.Controllers;

/// <summary>
/// Read-only dashboard and operational reporting endpoints.
/// All endpoints require the dashboard.read permission.
/// </summary>
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>
    /// Returns a single-call operational snapshot: sales, purchases, inventory, and alerts.
    /// </summary>
    /// <param name="from">Start date (inclusive). Defaults to first day of current month.</param>
    /// <param name="to">End date (inclusive). Defaults to today.</param>
    /// <param name="branchId">Optional branch filter.</param>
    /// <param name="warehouseId">Optional warehouse filter.</param>
    [HttpGet("summary")]
    [Authorize(Policy = PermissionNames.DashboardRead)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid?     branchId    = null,
        [FromQuery] Guid?     warehouseId = null,
        CancellationToken     ct          = default)
    {
        var today   = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? new DateOnly(today.Year, today.Month, 1);
        var toDate   = to   ?? today;

        var result = await _dashboard.GetSummaryAsync(fromDate, toDate, branchId, warehouseId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns current inventory snapshot: item counts, total quantity on hand, low-stock and out-of-stock counts.
    /// </summary>
    /// <param name="warehouseId">Optional warehouse filter.</param>
    [HttpGet("inventory")]
    [Authorize(Policy = PermissionNames.DashboardRead)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] Guid?     warehouseId = null,
        CancellationToken     ct          = default)
    {
        var result = await _dashboard.GetInventorySummaryAsync(warehouseId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns daily sales revenue and invoice count for the given period.
    /// Suitable for rendering a time-series chart.
    /// </summary>
    /// <param name="from">Start date (inclusive). Defaults to 30 days ago.</param>
    /// <param name="to">End date (inclusive). Defaults to today.</param>
    /// <param name="branchId">Optional branch filter.</param>
    /// <param name="warehouseId">Optional warehouse filter.</param>
    [HttpGet("sales-trend")]
    [Authorize(Policy = PermissionNames.DashboardRead)]
    public async Task<IActionResult> GetSalesTrend(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid?     branchId    = null,
        [FromQuery] Guid?     warehouseId = null,
        CancellationToken     ct          = default)
    {
        var today    = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? today.AddDays(-29);
        var toDate   = to   ?? today;

        var result = await _dashboard.GetSalesTrendAsync(fromDate, toDate, branchId, warehouseId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the top N items by quantity sold in the given period.
    /// </summary>
    /// <param name="from">Start date (inclusive). Defaults to first day of current month.</param>
    /// <param name="to">End date (inclusive). Defaults to today.</param>
    /// <param name="top">Number of items to return (1–50). Defaults to 10.</param>
    /// <param name="branchId">Optional branch filter.</param>
    /// <param name="warehouseId">Optional warehouse filter.</param>
    [HttpGet("top-items")]
    [Authorize(Policy = PermissionNames.DashboardRead)]
    public async Task<IActionResult> GetTopItems(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int       top         = 10,
        [FromQuery] Guid?     branchId    = null,
        [FromQuery] Guid?     warehouseId = null,
        CancellationToken     ct          = default)
    {
        if (top is < 1 or > 50)
            return BadRequest("top must be between 1 and 50.");

        var today    = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? new DateOnly(today.Year, today.Month, 1);
        var toDate   = to   ?? today;

        var result = await _dashboard.GetTopSellingItemsAsync(fromDate, toDate, top, branchId, warehouseId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns batches that are expired or expiring within the given number of days.
    /// Ordered by expiry date ascending (most urgent first).
    /// </summary>
    /// <param name="withinDays">Look-ahead window in days (1–365). Defaults to 30.</param>
    /// <param name="warehouseId">Optional warehouse filter.</param>
    [HttpGet("expiry-risk")]
    [Authorize(Policy = PermissionNames.DashboardRead)]
    public async Task<IActionResult> GetExpiryRisk(
        [FromQuery] int       withinDays  = 30,
        [FromQuery] Guid?     warehouseId = null,
        CancellationToken     ct          = default)
    {
        if (withinDays is < 1 or > 365)
            return BadRequest("withinDays must be between 1 and 365.");

        var result = await _dashboard.GetExpiryRiskAsync(withinDays, warehouseId, ct);
        return Ok(result);
    }
}

