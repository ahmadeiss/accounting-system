namespace Accounting.Core.Exceptions;

/// <summary>
/// Base exception for all domain/business rule violations.
/// These are expected exceptions with human-readable messages — NOT system errors.
/// They map to HTTP 422 Unprocessable Entity in the API layer.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Thrown when an attempt is made to sell from an expired batch.
/// </summary>
public class ExpiredBatchException : DomainException
{
    public Guid BatchId { get; }
    public DateOnly ExpiryDate { get; }

    public ExpiredBatchException(Guid batchId, DateOnly expiryDate)
        : base($"Batch '{batchId}' expired on {expiryDate:yyyy-MM-dd} and cannot be sold.", "EXPIRED_BATCH")
    {
        BatchId = batchId;
        ExpiryDate = expiryDate;
    }
}

/// <summary>
/// Thrown when there is insufficient stock to fulfill a sale or transfer.
/// </summary>
public class InsufficientStockException : DomainException
{
    public Guid ItemId { get; }
    public decimal Requested { get; }
    public decimal Available { get; }

    public InsufficientStockException(Guid itemId, decimal requested, decimal available)
        : base($"Insufficient stock for item '{itemId}'. Requested: {requested}, Available: {available}.", "INSUFFICIENT_STOCK")
    {
        ItemId = itemId;
        Requested = requested;
        Available = available;
    }
}

/// <summary>
/// Thrown when an entity is not found by the requested identifier.
/// Maps to HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}

/// <summary>
/// Thrown when a validation rule at the domain level is violated.
/// </summary>
public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR")
    {
        Errors = errors;
    }
}

/// <summary>
/// Thrown when a uniqueness constraint is violated (e.g., duplicate SKU).
/// </summary>
public class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string entityName, string fieldName, object value)
        : base($"{entityName} with {fieldName} '{value}' already exists.", "DUPLICATE_ENTITY")
    {
    }
}

/// <summary>
/// Thrown when an operation is attempted on an invoice in the wrong lifecycle state.
/// E.g. confirming a Confirmed or Cancelled invoice.
/// </summary>
public class InvalidInvoiceStatusException : DomainException
{
    public InvalidInvoiceStatusException(string invoiceNumber, string currentStatus, string requiredStatus)
        : base(
            $"Invoice '{invoiceNumber}' is in status '{currentStatus}'. Expected '{requiredStatus}'.",
            "INVALID_INVOICE_STATUS")
    {
    }
}

/// <summary>
/// Thrown when a batch-tracked purchase line is missing required batch/expiry data.
/// </summary>
public class MissingBatchDataException : DomainException
{
    public MissingBatchDataException(string itemName, string missingField)
        : base(
            $"Item '{itemName}' requires batch tracking. Field '{missingField}' must be provided on every purchase line.",
            "MISSING_BATCH_DATA")
    {
    }
}

/// <summary>
/// Thrown when authentication fails: bad credentials, inactive account, or invalid/expired token.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : Exception
{
    public string ErrorCode { get; }

    public UnauthorizedException(string message, string errorCode = "UNAUTHORIZED")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Thrown when an authenticated user lacks the required permission.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string permissionName)
        : base($"Permission required: '{permissionName}'.")
    {
    }
}

