namespace Accounting.Core.ValueObjects;

/// <summary>
/// Transient result from IBatchSelectionService.AllocateFEFOAsync.
/// Represents the decision to consume <see cref="Quantity"/> units from a specific batch.
/// Not persisted directly — the caller converts this into StockMovement +
/// SalesInvoiceLineAllocation records within the confirmation transaction.
/// </summary>
public record BatchAllocation(
    Guid BatchId,
    string BatchNumber,
    decimal Quantity,
    decimal UnitCost,
    DateOnly? ExpiryDate);

