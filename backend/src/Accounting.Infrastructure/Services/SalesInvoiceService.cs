using Accounting.Application.Sales.DTOs;
using Accounting.Application.Sales.Services;
using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Exceptions;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implements the full sales invoice lifecycle.
///
/// ConfirmAsync critical path:
///   1. Load invoice + lines + items (tracked).
///   2. Guard: must be Draft.
///   3. Begin transaction.
///   4. For each line:
///      a. Batch-tracked item  → AllocateFEFOAsync → one StockMovement + one
///         SalesInvoiceLineAllocation per batch consumed.
///      b. Non-batch item      → ValidateSufficientStockAsync → one StockMovement +
///         one SalesInvoiceLineAllocation (batchId = null).
///   5. Mark invoice Completed.
///   6. Write audit log.
///   7. SaveChanges + Commit.
/// </summary>
public class SalesInvoiceService : ISalesInvoiceService
{
    private readonly AccountingDbContext _context;
    private readonly IStockService _stock;
    private readonly IBatchSelectionService _batchSelector;
    private readonly IAuditService _audit;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<SalesInvoiceService> _logger;

    public SalesInvoiceService(
        AccountingDbContext context,
        IStockService stock,
        IBatchSelectionService batchSelector,
        IAuditService audit,
        IUnitOfWork uow,
        IMapper mapper,
        ILogger<SalesInvoiceService> logger)
    {
        _context = context;
        _stock = stock;
        _batchSelector = batchSelector;
        _audit = audit;
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── Get ─────────────────────────────────────────────────────────────────

    public async Task<SalesInvoiceDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _context.SalesInvoices
            .AsNoTracking()
            .Include(s => s.Branch)
            .Include(s => s.Warehouse)
            .Include(s => s.Customer)
            .Include(s => s.CreatedBy)
            .Include(s => s.Lines)
                .ThenInclude(l => l.Item)
            .Include(s => s.Lines)
                .ThenInclude(l => l.Allocations)
                    .ThenInclude(a => a.ItemBatch)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), id);

        return _mapper.Map<SalesInvoiceDto>(invoice);
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    public async Task<Guid> CreateAsync(
        CreateSalesInvoiceRequest request,
        Guid createdById,
        CancellationToken ct = default)
    {
        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var invoice = new SalesInvoice
        {
            InvoiceNumber = invoiceNumber,
            BranchId = request.BranchId,
            WarehouseId = request.WarehouseId,
            CustomerId = request.CustomerId,
            SaleDate = DateTime.UtcNow,
            PaymentMethod = request.PaymentMethod,
            PaidAmount = request.PaidAmount,
            Notes = request.Notes,
            Status = SalesInvoiceStatus.Draft,
            CreatedById = createdById
        };

        foreach (var lineReq in request.Lines)
        {
            var lineTotal = CalculateLineTotal(
                lineReq.Quantity, lineReq.UnitPrice,
                lineReq.DiscountPercent, lineReq.TaxPercent);

            invoice.Lines.Add(new SalesInvoiceLine
            {
                ItemId = lineReq.ItemId,
                Quantity = lineReq.Quantity,
                UnitPrice = lineReq.UnitPrice,
                DiscountPercent = lineReq.DiscountPercent,
                TaxPercent = lineReq.TaxPercent,
                LineTotal = lineTotal
            });
        }

        RecalculateTotals(invoice);

        await _uow.SalesInvoices.AddAsync(invoice, ct);

        await _audit.LogAsync(
            nameof(SalesInvoice), invoice.Id.ToString(),
            "CREATED", createdById,
            new { invoice.InvoiceNumber, LineCount = invoice.Lines.Count },
            ct);

        await _uow.SaveChangesAsync(ct);
        return invoice.Id;
    }

    // ─── Confirm ─────────────────────────────────────────────────────────────

    public async Task ConfirmAsync(Guid invoiceId, Guid confirmedById, CancellationToken ct = default)
    {
        var invoice = await _context.SalesInvoices
            .Include(s => s.Lines)
                .ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(s => s.Id == invoiceId, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), invoiceId);

        // ── Idempotency guard ────────────────────────────────────────────────
        if (invoice.Status != SalesInvoiceStatus.Draft)
            throw new InvalidInvoiceStatusException(
                invoice.InvoiceNumber,
                invoice.Status.ToString(),
                SalesInvoiceStatus.Draft.ToString());

        // Push structured properties into Serilog log context for the duration of Confirm.
        // Every log line emitted within this scope will carry InvoiceId and ConfirmedById.
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceId"] = invoiceId,
            ["InvoiceNumber"] = invoice.InvoiceNumber,
            ["ConfirmedById"] = confirmedById
        });

        _logger.LogInformation("Confirming sales invoice {InvoiceNumber} ({InvoiceId})",
            invoice.InvoiceNumber, invoiceId);

        // SERIALIZABLE prevents two concurrent sales from both reading sufficient stock
        // and both committing — one will be aborted by PostgreSQL with a serialization error,
        // which surfaces as InsufficientStockException after retry or as a 409 to the client.
        await _uow.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try
        {
            await ProcessLinesAsync(invoice, confirmedById, ct);

            invoice.Status = SalesInvoiceStatus.Completed;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _audit.LogAsync(
                nameof(SalesInvoice), invoice.Id.ToString(),
                "CONFIRMED", confirmedById,
                new
                {
                    invoice.InvoiceNumber,
                    invoice.TotalAmount,
                    LineCount = invoice.Lines.Count,
                    invoice.WarehouseId
                },
                ct);

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task ProcessLinesAsync(
        SalesInvoice invoice, Guid confirmedById, CancellationToken ct)
    {
        foreach (var line in invoice.Lines)
        {
            var item = line.Item;

            if (item.TrackBatch)
            {
                // FEFO allocation — may span multiple batches
                var allocations = await _batchSelector.AllocateFEFOAsync(
                    item.Id, invoice.WarehouseId, line.Quantity, ct);

                foreach (var alloc in allocations)
                {
                    // Deduct stock through StockService (negative quantity = stock out)
                    await _stock.RecordMovementAsync(
                        itemId: item.Id,
                        warehouseId: invoice.WarehouseId,
                        quantity: -alloc.Quantity,
                        movementType: StockMovementType.Sale,
                        referenceType: nameof(SalesInvoice),
                        referenceId: invoice.Id,
                        itemBatchId: alloc.BatchId,
                        unitCost: alloc.UnitCost,
                        notes: $"Sale — {invoice.InvoiceNumber}",
                        createdById: confirmedById,
                        ct: ct);

                    // Persist allocation record for full traceability
                    var allocationRecord = new SalesInvoiceLineAllocation
                    {
                        SalesInvoiceLineId = line.Id,
                        ItemBatchId = alloc.BatchId,
                        Quantity = alloc.Quantity,
                        UnitCost = alloc.UnitCost,
                        ExpiryDateSnapshot = alloc.ExpiryDate
                    };
                    await _context.SalesInvoiceLineAllocations.AddAsync(allocationRecord, ct);
                }

                // Set ItemBatchId on the line to the first (primary) batch for backward compat
                line.ItemBatchId = allocations[0].BatchId;
            }
            else
            {
                // Non-batch item: validate total stock balance, then record movement
                await _stock.ValidateSufficientStockAsync(
                    item.Id, invoice.WarehouseId, line.Quantity, ct);

                await _stock.RecordMovementAsync(
                    itemId: item.Id,
                    warehouseId: invoice.WarehouseId,
                    quantity: -line.Quantity,
                    movementType: StockMovementType.Sale,
                    referenceType: nameof(SalesInvoice),
                    referenceId: invoice.Id,
                    itemBatchId: null,
                    unitCost: item.CostPrice,
                    notes: $"Sale — {invoice.InvoiceNumber}",
                    createdById: confirmedById,
                    ct: ct);

                // Single allocation record with null batch for non-batch items
                var allocationRecord = new SalesInvoiceLineAllocation
                {
                    SalesInvoiceLineId = line.Id,
                    ItemBatchId = null,
                    Quantity = line.Quantity,
                    UnitCost = item.CostPrice,
                    ExpiryDateSnapshot = null
                };
                await _context.SalesInvoiceLineAllocations.AddAsync(allocationRecord, ct);
            }
        }
    }

    private static void RecalculateTotals(SalesInvoice invoice)
    {
        invoice.SubTotal = invoice.Lines.Sum(l => Math.Round(l.Quantity * l.UnitPrice, 4));
        invoice.DiscountAmount = invoice.Lines.Sum(l =>
            Math.Round(l.Quantity * l.UnitPrice * l.DiscountPercent / 100, 4));
        invoice.TaxAmount = invoice.Lines.Sum(l =>
        {
            var afterDiscount = l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100);
            return Math.Round(afterDiscount * l.TaxPercent / 100, 4);
        });
        invoice.TotalAmount = invoice.SubTotal - invoice.DiscountAmount + invoice.TaxAmount;
    }

    private static decimal CalculateLineTotal(
        decimal qty, decimal unitPrice, decimal discountPct, decimal taxPct)
    {
        var subtotal = qty * unitPrice;
        var discount = Math.Round(subtotal * discountPct / 100, 4);
        var afterDiscount = subtotal - discount;
        var tax = Math.Round(afterDiscount * taxPct / 100, 4);
        return afterDiscount + tax;
    }

    /// <summary>
    /// Uses a PostgreSQL sequence (sales_invoice_seq) to generate a unique, gap-free
    /// invoice number. Safe under concurrent creates — NEXTVAL is atomic.
    /// Format: SAL-{YEAR}-{SEQ:D5}  e.g. SAL-2026-00007
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var seq = await _context.Database
            .SqlQueryRaw<long>("SELECT NEXTVAL('sales_invoice_seq') AS \"Value\"")
            .FirstAsync(ct);
        return $"SAL-{year}-{seq:D5}";
    }
}

