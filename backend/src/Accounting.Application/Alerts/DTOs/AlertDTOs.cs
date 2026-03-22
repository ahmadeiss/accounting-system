using Accounting.Core.Enums;

namespace Accounting.Application.Alerts.DTOs;

// ─── Response ────────────────────────────────────────────────────────────────

/// <summary>
/// Lightweight alert projection returned by GET /api/v1/alerts.
/// </summary>
public record AlertDto(
    Guid Id,
    string AlertType,
    string Severity,
    string Status,
    string Message,
    Guid? ItemId,
    string? ItemName,
    string? ItemSku,
    Guid? ItemBatchId,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    Guid? WarehouseId,
    string? WarehouseName,
    string? Metadata,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ─── Query ───────────────────────────────────────────────────────────────────

/// <summary>Query filters for GET /api/v1/alerts.</summary>
public record AlertListRequest(
    AlertType? Type = null,
    AlertStatus? Status = null,
    AlertSeverity? Severity = null);

