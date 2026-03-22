namespace Accounting.Application.Purchasing.DTOs;

/// <summary>
/// Lightweight row for the purchase invoice list table.
/// Does not include lines — call GetById for full detail.
/// </summary>
public record PurchaseInvoiceListDto(
    Guid     Id,
    string   InvoiceNumber,
    string   SupplierName,
    string   WarehouseName,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string   Status,
    int      LineCount,
    decimal  TotalAmount,
    DateTime CreatedAt);

public record PurchaseInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string SupplierName,
    Guid SupplierId,
    string BranchName,
    string WarehouseName,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string Status,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    string? Notes,
    string CreatedByName,
    DateTime CreatedAt,
    IReadOnlyList<PurchaseInvoiceLineDto> Lines);

public record PurchaseInvoiceLineDto(
    Guid Id,
    Guid ItemId,
    string ItemName,
    string ItemSKU,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountPercent,
    decimal TaxPercent,
    decimal LineTotal,
    string? BatchNumber,
    DateOnly? ProductionDate,
    DateOnly? ExpiryDate,
    string? Notes);

public record CreatePurchaseInvoiceRequest(
    Guid SupplierId,
    Guid BranchId,
    Guid WarehouseId,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string? Notes,
    IReadOnlyList<CreatePurchaseInvoiceLineRequest> Lines);

public record CreatePurchaseInvoiceLineRequest(
    Guid ItemId,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountPercent,
    decimal TaxPercent,
    string? BatchNumber,
    DateOnly? ProductionDate,
    DateOnly? ExpiryDate,
    string? Notes);

public record ConfirmPurchaseInvoiceRequest(Guid InvoiceId);

