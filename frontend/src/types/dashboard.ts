// Mirrors Accounting.Application.Dashboard.DTOs

export interface SalesSummary {
  invoiceCount: number;
  totalRevenue: number;
  totalTax: number;
  totalDiscount: number;
  averageOrderValue: number;
}

export interface PurchaseSummary {
  invoiceCount: number;
  totalCost: number;
  totalTax: number;
  totalDiscount: number;
  balanceDue: number;
}

export interface InventorySummary {
  distinctItems: number;
  distinctWarehouses: number;
  totalQuantityOnHand: number;
  lowStockItemCount: number;
  outOfStockItemCount: number;
}

export interface AlertSummary {
  totalActive: number;
  lowStock: number;
  nearExpiry: number;
  expiredStock: number;
  batchRecalled: number;
}

export interface DashboardSummary {
  sales: SalesSummary;
  purchases: PurchaseSummary;
  inventory: InventorySummary;
  alerts: AlertSummary;
}

export interface DailySales {
  date: string; // "YYYY-MM-DD"
  invoiceCount: number;
  revenue: number;
}

export interface TopSellingItem {
  itemId: string;
  itemName: string;
  itemCode: string;
  quantitySold: number;
  revenue: number;
}

export interface ExpiryRisk {
  batchId: string;
  batchNumber: string;
  itemId: string;
  itemName: string;
  warehouseId: string;
  warehouseName: string;
  expiryDate: string; // "YYYY-MM-DD"
  daysUntilExpiry: number;
  availableQuantity: number;
  isExpired: boolean;
}

