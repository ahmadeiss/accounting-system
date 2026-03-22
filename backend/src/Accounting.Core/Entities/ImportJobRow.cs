using Accounting.Core.Enums;

namespace Accounting.Core.Entities;

public class ImportJobRow : BaseEntity
{
    public Guid ImportJobId { get; set; }
    public ImportJob ImportJob { get; set; } = null!;

    public int RowNumber { get; set; }
    public ImportRowStatus Status { get; set; }

    /// <summary>JSON-serialized raw row data from the Excel file.</summary>
    public string RawData { get; set; } = string.Empty;

    /// <summary>Validation or processing error message for this row.</summary>
    public string? ErrorMessage { get; set; }
}

