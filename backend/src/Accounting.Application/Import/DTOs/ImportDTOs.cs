using Accounting.Core.Enums;

namespace Accounting.Application.Import.DTOs;

// ─── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Submitted by the controller when the user uploads an Excel file for item master import.
/// </summary>
public record ImportItemsRequest(
    /// <summary>Raw bytes of the uploaded .xlsx file.</summary>
    byte[] FileBytes,
    string OriginalFileName,
    Guid CreatedById,
    /// <summary>When true, validate all rows but do not persist anything.</summary>
    bool DryRun = false
);

/// <summary>
/// Submitted by the controller when the user uploads an Excel file for opening stock import.
/// </summary>
public record ImportOpeningStockRequest(
    byte[] FileBytes,
    string OriginalFileName,
    Guid WarehouseId,
    Guid CreatedById,
    bool DryRun = false
);

// ─── Row-level result ─────────────────────────────────────────────────────────

public record ImportRowResult(
    int RowNumber,
    ImportRowStatus Status,
    string? ErrorMessage,
    /// <summary>Snapshot of the raw data parsed from this row (for audit/display).</summary>
    string RawData
);

// ─── Job-level result ─────────────────────────────────────────────────────────

public record ImportResult(
    Guid? JobId,
    bool IsDryRun,
    ImportJobStatus Status,
    int TotalRows,
    int SuccessRows,
    int FailedRows,
    int SkippedRows,
    string? ErrorSummary,
    IReadOnlyList<ImportRowResult> Rows
)
{
    public bool HasErrors => FailedRows > 0;
}

