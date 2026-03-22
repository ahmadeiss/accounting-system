using Accounting.Application.Purchasing.DTOs;

namespace Accounting.Application.Purchasing.Services;

/// <summary>
/// Application-level operations for purchase invoices.
/// All mutations are transactional and go through IStockService for stock changes.
/// </summary>
public interface IPurchaseInvoiceService
{
    /// <summary>
    /// Creates a new purchase invoice in Draft status.
    /// Returns the new invoice ID.
    /// </summary>
    Task<Guid> CreateAsync(CreatePurchaseInvoiceRequest request, Guid createdById, CancellationToken ct = default);

    /// <summary>
    /// Returns a fully populated purchase invoice DTO including lines and totals.
    /// Throws NotFoundException if not found.
    /// </summary>
    Task<PurchaseInvoiceDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Confirms a Draft invoice:
    ///   - validates all lines
    ///   - creates ItemBatch records for batch/expiry-tracked items
    ///   - records a PurchaseReceipt stock movement per line via IStockService
    ///   - writes audit log entries
    ///   - transitions status to Confirmed
    /// Throws InvalidInvoiceStatusException if invoice is not in Draft.
    /// Throws MissingBatchDataException if a batch-tracked line is missing batch/expiry data.
    /// All operations are atomic in a single DB transaction.
    /// </summary>
    Task ConfirmAsync(Guid invoiceId, Guid confirmedById, CancellationToken ct = default);
}

