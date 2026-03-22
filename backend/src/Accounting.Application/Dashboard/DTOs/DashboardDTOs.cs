namespace Accounting.Application.Dashboard.DTOs;

// ─── Top-level summary ────────────────────────────────────────────────────────

/// <summary>
/// Single-call operational snapshot for the manager home dashboard.
/// All monetary values are in the system's base currency.
/// All counts and amounts reflect CONFIRMED/COMPLETED records only.
/// </summary>
public record DashboardSummaryDto(
    SalesSummaryDto    Sales,
    PurchaseSummaryDto Purchases,
    InventorySummaryDto Inventory,
    AlertSummaryDto    Alerts
);

// ─── Sales ────────────────────────────────────────────────────────────────────

/// <summary>Sales figures for the requested period (Completed invoices only).</summary>
public record SalesSummaryDto(
    int     InvoiceCount,
    decimal TotalRevenue,
    decimal TotalTax,
    decimal TotalDiscount,
    decimal AverageOrderValue
);

// ─── Purchases ────────────────────────────────────────────────────────────────

/// <summary>Purchase figures for the requested period (Confirmed invoices only).</summary>
public record PurchaseSummaryDto(
    int     InvoiceCount,
    decimal TotalCost,
    decimal TotalTax,
    decimal TotalDiscount,
    decimal BalanceDue          // sum of (TotalAmount - PaidAmount) for confirmed invoices
);

// ─── Inventory ────────────────────────────────────────────────────────────────

/// <summary>Current inventory snapshot (point-in-time, not period-bound).</summary>
public record InventorySummaryDto(
    int     DistinctItems,          // items with at least one StockBalance row
    int     DistinctWarehouses,     // warehouses that hold stock
    decimal TotalQuantityOnHand,
    int     LowStockItemCount,      // items where QuantityOnHand ≤ Item.ReorderLevel
    int     OutOfStockItemCount     // items where QuantityOnHand = 0
);

// ─── Alerts ───────────────────────────────────────────────────────────────────

/// <summary>Active alert counts by type (Active + Acknowledged only).</summary>
public record AlertSummaryDto(
    int TotalActive,
    int LowStock,
    int NearExpiry,
    int ExpiredStock,
    int BatchRecalled
);

// ─── Sales trend ─────────────────────────────────────────────────────────────

/// <summary>One data point per day for a time-series chart.</summary>
public record DailySalesDto(
    DateOnly Date,
    int      InvoiceCount,
    decimal  Revenue
);

// ─── Top-selling items ────────────────────────────────────────────────────────

/// <summary>Item ranked by quantity sold in the requested period.</summary>
public record TopSellingItemDto(
    Guid    ItemId,
    string  ItemName,
    string  ItemCode,
    decimal QuantitySold,
    decimal Revenue
);

// ─── Expiry risk ─────────────────────────────────────────────────────────────

/// <summary>Batch-level expiry risk for proactive stock management.</summary>
public record ExpiryRiskDto(
    Guid      BatchId,
    string    BatchNumber,
    Guid      ItemId,
    string    ItemName,
    Guid      WarehouseId,
    string    WarehouseName,
    DateOnly  ExpiryDate,
    int       DaysUntilExpiry,
    decimal   AvailableQuantity,
    bool      IsExpired
);

