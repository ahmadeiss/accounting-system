using Accounting.Application.Sales.DTOs;

namespace Accounting.Application.Sales.Services;

/// <summary>
/// Sales invoice application service.
/// Orchestrates validation, FEFO allocation, stock movements, and auditing.
/// </summary>
public interface ISalesInvoiceService
{
    /// <summary>Returns a confirmed or draft sales invoice with all lines and allocations.</summary>
    Task<SalesInvoiceDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new sales invoice in Draft status.
    /// Stock is NOT touched at this stage.
    /// </summary>
    Task<Guid> CreateAsync(CreateSalesInvoiceRequest request, Guid createdById, CancellationToken ct = default);

    /// <summary>
    /// Confirms a Draft invoice:
    ///   1. Validates all lines.
    ///   2. Allocates stock via FEFO for batch-tracked items.
    ///   3. Calls StockService.RecordMovementAsync for each allocation.
    ///   4. Creates SalesInvoiceLineAllocation records.
    ///   5. Writes audit log.
    ///   6. Everything in one database transaction.
    /// Throws InvalidInvoiceStatusException if invoice is not Draft.
    /// Throws InsufficientStockException if any line cannot be fulfilled.
    /// </summary>
    Task ConfirmAsync(Guid invoiceId, Guid confirmedById, CancellationToken ct = default);
}

