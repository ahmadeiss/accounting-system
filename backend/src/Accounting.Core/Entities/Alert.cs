using Accounting.Core.Enums;

namespace Accounting.Core.Entities;

/// <summary>
/// Represents a system-generated operational alert.
///
/// Lifecycle: Active → Acknowledged → Resolved
///
/// Deduplication key:
///   - LowStock   : (AlertType, ItemId, WarehouseId, Status=Active)
///   - NearExpiry : (AlertType, ItemBatchId, Status=Active)
///   - Expired    : (AlertType, ItemBatchId, Status=Active)
///
/// All alert creation and lifecycle transitions must go through IAlertService.
/// </summary>
public class Alert : BaseEntity
{
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }

    /// <summary>Active → Acknowledged → Resolved</summary>
    public AlertStatus Status { get; set; } = AlertStatus.Active;

    public string Message { get; set; } = string.Empty;

    /// <summary>Optional JSON blob for structured data (quantities, dates, thresholds).</summary>
    public string? Metadata { get; set; }

    // ─── Context references (nullable — depends on alert type) ────────────────

    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public Guid? ItemBatchId { get; set; }
    public ItemBatch? ItemBatch { get; set; }

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}

