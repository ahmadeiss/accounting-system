namespace Accounting.Core.Enums;

public enum ImportJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    PartialSuccess = 4,
    Failed = 5
}

public enum ImportJobType
{
    ItemImport = 1,
    OpeningStockImport = 2,
    SupplierImport = 3,
    CustomerImport = 4
}

public enum ImportRowStatus
{
    Success = 1,
    Failed = 2,
    Skipped = 3
}

