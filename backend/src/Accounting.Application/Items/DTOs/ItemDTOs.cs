namespace Accounting.Application.Items.DTOs;

public record ItemDto(
    Guid Id,
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    string CategoryName,
    Guid CategoryId,
    string UnitName,
    string UnitAbbreviation,
    Guid UnitId,
    decimal CostPrice,
    decimal SalePrice,
    decimal ReorderLevel,
    bool TrackBatch,
    bool TrackExpiry,
    int MinExpiryDaysBeforeSale,
    bool IsActive,
    DateTime CreatedAt);

public record CreateItemRequest(
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    Guid CategoryId,
    Guid UnitId,
    decimal CostPrice,
    decimal SalePrice,
    decimal ReorderLevel,
    bool TrackBatch,
    bool TrackExpiry,
    int MinExpiryDaysBeforeSale);

public record UpdateItemRequest(
    string Name,
    string? Barcode,
    string? Description,
    Guid CategoryId,
    Guid UnitId,
    decimal CostPrice,
    decimal SalePrice,
    decimal ReorderLevel,
    bool TrackBatch,
    bool TrackExpiry,
    int MinExpiryDaysBeforeSale,
    bool IsActive);

public record ItemStockSummaryDto(
    Guid ItemId,
    string ItemName,
    string SKU,
    Guid WarehouseId,
    string WarehouseName,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    bool IsBelowReorder);

public record ItemBatchDto(
    Guid Id,
    string BatchNumber,
    DateOnly? ProductionDate,
    DateOnly? ExpiryDate,
    decimal ReceivedQuantity,
    decimal AvailableQuantity,
    decimal CostPerUnit,
    string Status,
    string? Notes,
    bool IsExpired,
    int? DaysUntilExpiry);

