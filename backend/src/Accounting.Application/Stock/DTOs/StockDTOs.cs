using Accounting.Core.Enums;

namespace Accounting.Application.Stock.DTOs;

public record StockMovementDto(
    Guid Id,
    string ItemName,
    string ItemSKU,
    string? BatchNumber,
    string WarehouseName,
    string MovementType,
    decimal Quantity,
    decimal UnitCost,
    string ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    DateTime MovementDate,
    string CreatedByName);

public record StockAdjustmentRequest(
    Guid ItemId,
    Guid WarehouseId,
    Guid? ItemBatchId,
    decimal Quantity,
    decimal UnitCost,
    string Reason,
    string? Notes);

public record StockTransferRequest(
    Guid ItemId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    Guid? ItemBatchId,
    decimal Quantity,
    string? Notes);

public record StockBalanceDto(
    Guid ItemId,
    string ItemName,
    string SKU,
    Guid WarehouseId,
    string WarehouseName,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    bool IsBelowReorder,
    DateTime LastUpdated);

