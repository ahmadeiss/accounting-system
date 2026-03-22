using Accounting.Core.Enums;

namespace Accounting.Core.Entities;

public class ImportJob : BaseEntity
{
    public ImportJobType JobType { get; set; }
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFilePath { get; set; } = string.Empty;

    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }
    public int SkippedRows { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ErrorSummary { get; set; }

    /// <summary>
    /// When true, the import was executed in dry-run mode: all rows were validated
    /// but no data was persisted. No stock movements or entity records were created.
    /// </summary>
    public bool IsDryRun { get; set; }

    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    // Navigation
    public ICollection<ImportJobRow> Rows { get; set; } = new List<ImportJobRow>();
}

