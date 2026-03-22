using Accounting.Application.Purchasing.DTOs;
using Accounting.Application.Purchasing.Services;
using Accounting.Core.Entities;
using Accounting.Core.Enums;
using Accounting.Core.Exceptions;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implements the full purchase invoice lifecycle.
/// ConfirmAsync is the critical path: validates, creates batches, records stock
/// movements, writes audit, all in one atomic transaction.
/// </summary>
public class PurchaseInvoiceService : Application.Purchasing.Services.IPurchaseInvoiceService
{
    private readonly AccountingDbContext _context;
    private readonly IStockService _stock;
    private readonly IAuditService _audit;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<PurchaseInvoiceService> _logger;

    public PurchaseInvoiceService(
        AccountingDbContext context,
        IStockService stock,
        IAuditService audit,
        IUnitOfWork uow,
        IMapper mapper,
        ILogger<PurchaseInvoiceService> logger)
    {
        _context = context;
        _stock = stock;
        _audit = audit;
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    public async Task<Guid> CreateAsync(
        CreatePurchaseInvoiceRequest request,
        Guid createdById,
        CancellationToken ct = default)
    {
        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var invoice = new PurchaseInvoice
        {
            InvoiceNumber = invoiceNumber,
            SupplierId = request.SupplierId,
            BranchId = request.BranchId,
            WarehouseId = request.WarehouseId,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            Notes = request.Notes,
            Status = PurchaseInvoiceStatus.Draft,
            CreatedById = createdById
        };

        foreach (var lineReq in request.Lines)
        {
            var lineTotal = CalculateLineTotal(lineReq.Quantity, lineReq.UnitCost,
                lineReq.DiscountPercent, lineReq.TaxPercent);

            invoice.Lines.Add(new PurchaseInvoiceLine
            {
                ItemId = lineReq.ItemId,
                Quantity = lineReq.Quantity,
                UnitCost = lineReq.UnitCost,
                DiscountPercent = lineReq.DiscountPercent,
                TaxPercent = lineReq.TaxPercent,
                LineTotal = lineTotal,
                BatchNumber = lineReq.BatchNumber,
                ProductionDate = lineReq.ProductionDate,
                ExpiryDate = lineReq.ExpiryDate,
                Notes = lineReq.Notes
            });
        }

        RecalculateTotals(invoice);

        await _uow.PurchaseInvoices.AddAsync(invoice, ct);

        await _audit.LogAsync(
            nameof(PurchaseInvoice), invoice.Id.ToString(),
            "CREATED", createdById,
            new { invoice.InvoiceNumber, LineCount = invoice.Lines.Count },
            ct);

        await _uow.SaveChangesAsync(ct);
        return invoice.Id;
    }

    // ─── Get ─────────────────────────────────────────────────────────────────

    public async Task<PurchaseInvoiceDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _context.PurchaseInvoices
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Branch)
            .Include(p => p.Warehouse)
            .Include(p => p.CreatedBy)
            .Include(p => p.Lines)
                .ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), id);

        return _mapper.Map<PurchaseInvoiceDto>(invoice);
    }

    // ─── Confirm ─────────────────────────────────────────────────────────────

    public async Task ConfirmAsync(Guid invoiceId, Guid confirmedById, CancellationToken ct = default)
    {
        // Load with tracking — we will mutate lines and the invoice
        var invoice = await _context.PurchaseInvoices
            .Include(p => p.Lines)
                .ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(p => p.Id == invoiceId, ct)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), invoiceId);

        // ── Idempotency guard ────────────────────────────────────────────────
        if (invoice.Status != PurchaseInvoiceStatus.Draft)
            throw new InvalidInvoiceStatusException(
                invoice.InvoiceNumber,
                invoice.Status.ToString(),
                PurchaseInvoiceStatus.Draft.ToString());

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceId"] = invoiceId,
            ["InvoiceNumber"] = invoice.InvoiceNumber,
            ["ConfirmedById"] = confirmedById
        });

        _logger.LogInformation("Confirming purchase invoice {InvoiceNumber} ({InvoiceId})",
            invoice.InvoiceNumber, invoiceId);

        // SERIALIZABLE: prevents concurrent receipts of the same batch from creating
        // duplicate StockBalance rows or double-counting AvailableQuantity.
        await _uow.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try
        {
            await ProcessLinesAsync(invoice, confirmedById, ct);

            invoice.Status = PurchaseInvoiceStatus.Confirmed;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _audit.LogAsync(
                nameof(PurchaseInvoice), invoice.Id.ToString(),
                "CONFIRMED", confirmedById,
                new
                {
                    invoice.InvoiceNumber,
                    invoice.TotalAmount,
                    LineCount = invoice.Lines.Count,
                    WarehouseId = invoice.WarehouseId
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
        PurchaseInvoice invoice, Guid confirmedById, CancellationToken ct)
    {
        foreach (var line in invoice.Lines)
        {
            var item = line.Item;

            // ── Batch / Expiry validation ────────────────────────────────────
            if (item.TrackBatch)
            {
                if (string.IsNullOrWhiteSpace(line.BatchNumber))
                    throw new MissingBatchDataException(item.Name, "BatchNumber");

                if (item.TrackExpiry && !line.ExpiryDate.HasValue)
                    throw new MissingBatchDataException(item.Name, "ExpiryDate");

                if (item.TrackExpiry && line.ExpiryDate.HasValue
                    && line.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
                    throw new DomainException(
                        $"ExpiryDate on line for item '{item.Name}' must be in the future.",
                        "EXPIRY_DATE_IN_PAST");

                var batch = await ResolveOrCreateBatchAsync(line, invoice.WarehouseId, confirmedById, ct);
                line.ItemBatchId = batch.Id;

                await _stock.RecordMovementAsync(
                    itemId: item.Id,
                    warehouseId: invoice.WarehouseId,
                    quantity: line.Quantity,          // positive = stock in
                    movementType: StockMovementType.Purchase,
                    referenceType: nameof(PurchaseInvoice),
                    referenceId: invoice.Id,
                    itemBatchId: batch.Id,
                    unitCost: line.UnitCost,
                    notes: $"Purchase receipt — {invoice.InvoiceNumber}",
                    createdById: confirmedById,
                    ct: ct);
            }
            else
            {
                // Non-batch item: no ItemBatch record, movement without batchId
                line.ItemBatchId = null;

                await _stock.RecordMovementAsync(
                    itemId: item.Id,
                    warehouseId: invoice.WarehouseId,
                    quantity: line.Quantity,
                    movementType: StockMovementType.Purchase,
                    referenceType: nameof(PurchaseInvoice),
                    referenceId: invoice.Id,
                    itemBatchId: null,
                    unitCost: line.UnitCost,
                    notes: $"Purchase receipt — {invoice.InvoiceNumber}",
                    createdById: confirmedById,
                    ct: ct);
            }
        }
    }

    /// <summary>
    /// Finds an existing Active batch with the same BatchNumber+Item+Warehouse.
    /// If found, appends the received quantity (same physical batch, multiple deliveries).
    /// If not found, creates a new ItemBatch record.
    /// </summary>
    private async Task<ItemBatch> ResolveOrCreateBatchAsync(
        PurchaseInvoiceLine line, Guid warehouseId, Guid createdById, CancellationToken ct)
    {
        var existing = await _context.ItemBatches
            .FirstOrDefaultAsync(b =>
                b.ItemId == line.ItemId &&
                b.WarehouseId == warehouseId &&
                b.BatchNumber == line.BatchNumber &&
                b.Status == BatchStatus.Active,
                ct);

        if (existing is not null)
        {
            // Same physical batch arriving in multiple shipments — accumulate
            existing.ReceivedQuantity += line.Quantity;
            // AvailableQuantity will be updated by StockService.RecordMovementAsync
            return existing;
        }

        var batch = new ItemBatch
        {
            ItemId = line.ItemId,
            WarehouseId = warehouseId,
            BatchNumber = line.BatchNumber!,
            ProductionDate = line.ProductionDate,
            ExpiryDate = line.ExpiryDate,
            ReceivedQuantity = line.Quantity,
            AvailableQuantity = 0,   // Will be incremented by StockService
            CostPerUnit = line.UnitCost,
            Status = BatchStatus.Active,
            Notes = line.Notes
        };

        // PK is already a client-side Guid (BaseEntity sets Id = Guid.NewGuid()).
        // No intermediate SaveChanges needed — the Id is available immediately.
        await _context.ItemBatches.AddAsync(batch, ct);

        return batch;
    }

    private static void RecalculateTotals(PurchaseInvoice invoice)
    {
        invoice.SubTotal = invoice.Lines.Sum(l => Math.Round(l.Quantity * l.UnitCost, 4));
        invoice.DiscountAmount = invoice.Lines.Sum(l => l.DiscountAmount);
        invoice.TaxAmount = invoice.Lines.Sum(l => l.TaxAmount);
        invoice.TotalAmount = invoice.SubTotal - invoice.DiscountAmount + invoice.TaxAmount;
    }

    private static decimal CalculateLineTotal(
        decimal qty, decimal unitCost, decimal discountPct, decimal taxPct)
    {
        var subtotal = qty * unitCost;
        var discount = Math.Round(subtotal * discountPct / 100, 4);
        var afterDiscount = subtotal - discount;
        var tax = Math.Round(afterDiscount * taxPct / 100, 4);
        return afterDiscount + tax;
    }

    /// <summary>
    /// Uses a PostgreSQL sequence (purchase_invoice_seq) to generate a unique, gap-free
    /// invoice number. Safe under concurrent creates — NEXTVAL is atomic.
    /// Format: PUR-{YEAR}-{SEQ:D5}  e.g. PUR-2026-00042
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var seq = await _context.Database
            .SqlQueryRaw<long>("SELECT NEXTVAL('purchase_invoice_seq') AS \"Value\"")
            .FirstAsync(ct);
        return $"PUR-{year}-{seq:D5}";
    }
}

