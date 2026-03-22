using Accounting.Core.Entities;
using Accounting.Core.Enums;

namespace Accounting.Core.Interfaces;

/// <summary>
/// Alert lifecycle manager.
///
/// Responsibilities:
/// - Create alerts with deduplication (never duplicate an Active alert for the same condition).
/// - Transition alerts: Active → Acknowledged → Resolved.
/// - Auto-resolve stale alerts when conditions are no longer true.
/// - Provide queryable alert list for the API.
///
/// All implementations must NOT call SaveChangesAsync internally.
/// The caller (AlertScanner or controller action) owns the transaction boundary.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Creates a new alert or updates the timestamp of an existing Active one.
    ///
    /// Deduplication key:
    ///   LowStock  → (AlertType, ItemId, WarehouseId, Status=Active)
    ///   NearExpiry/Expired → (AlertType, ItemBatchId, Status=Active)
    ///
    /// Returns the alert (new or existing).
    /// </summary>
    Task<Alert> CreateOrUpdateAlertAsync(
        AlertType type,
        AlertSeverity severity,
        string message,
        Guid? itemId,
        Guid? itemBatchId,
        Guid? warehouseId,
        string? metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Transitions an Active alert to Acknowledged.
    /// Throws InvalidOperationException if alert is already Resolved.
    /// </summary>
    Task AcknowledgeAsync(Guid alertId, CancellationToken ct = default);

    /// <summary>
    /// Transitions an alert (Active or Acknowledged) to Resolved.
    /// </summary>
    Task ResolveAsync(Guid alertId, CancellationToken ct = default);

    /// <summary>
    /// Queries alerts with optional filters.
    /// </summary>
    Task<IReadOnlyList<Alert>> GetAlertsAsync(
        AlertType? type = null,
        AlertStatus? status = null,
        AlertSeverity? severity = null,
        CancellationToken ct = default);

    /// <summary>
    /// For a given AlertType, resolves all Active alerts whose IDs are NOT in
    /// <paramref name="stillActiveIds"/> (i.e., the condition no longer exists).
    ///
    /// Called by AlertScanner after each scan pass to clean up stale alerts.
    /// </summary>
    Task AutoResolveStaleAsync(
        AlertType type,
        IEnumerable<Guid> stillActiveIds,
        CancellationToken ct = default);
}

