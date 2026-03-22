using Accounting.Core.Enums;

namespace Accounting.Application.Sales.DTOs;

public record SalesInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string BranchName,
    string WarehouseName,
    string? CustomerName,
    DateTime SaleDate,
    string Status,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal ChangeAmount,
    string PaymentMethod,
    string? Notes,
    string CreatedByName,
    IReadOnlyList<SalesInvoiceLineDto> Lines);

/// <summary>
/// A single batch allocation within a confirmed sales invoice line.
/// For non-batch items: one record with null BatchNumber / ExpiryDateSnapshot.
/// For batch items: one record per batch consumed (FEFO multi-batch splits).
/// </summary>
public record SalesInvoiceLineAllocationDto(
    Guid Id,
    Guid? ItemBatchId,
    string? BatchNumber,
    DateOnly? ExpiryDateSnapshot,
    decimal Quantity,
    decimal UnitCost);

/// <summary>
/// A sales invoice line. Batch traceability is exposed through
/// <see cref="Allocations"/> which supports multi-batch FEFO splits.
/// </summary>
public record SalesInvoiceLineDto(
    Guid Id,
    Guid ItemId,
    string ItemName,
    string ItemSKU,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    decimal LineTotal,
    IReadOnlyList<SalesInvoiceLineAllocationDto> Allocations);

public record CreateSalesInvoiceRequest(
    Guid BranchId,
    Guid WarehouseId,
    Guid? CustomerId,
    PaymentMethod PaymentMethod,
    decimal PaidAmount,
    string? Notes,
    IReadOnlyList<CreateSalesInvoiceLineRequest> Lines);

public record CreateSalesInvoiceLineRequest(
    Guid ItemId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent);

