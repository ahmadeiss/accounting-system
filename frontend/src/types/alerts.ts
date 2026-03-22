// Mirrors Accounting.Application.Alerts.DTOs and Accounting.Core.Enums

export type AlertType = 'LowStock' | 'NearExpiry' | 'ExpiredStock' | 'BatchRecalled';
export type AlertStatus = 'Active' | 'Acknowledged' | 'Resolved';
export type AlertSeverity = 'Info' | 'Warning' | 'Critical';

export interface Alert {
  id: string;
  alertType: AlertType;
  severity: AlertSeverity;
  status: AlertStatus;
  message: string;
  itemId: string | null;
  itemName: string | null;
  itemSku: string | null;
  itemBatchId: string | null;
  batchNumber: string | null;
  expiryDate: string | null; // "YYYY-MM-DD"
  warehouseId: string | null;
  warehouseName: string | null;
  metadata: string | null;
  createdAt: string; // ISO datetime
  updatedAt: string; // ISO datetime
}

export interface AlertFilters {
  type?: AlertType;
  status?: AlertStatus;
  severity?: AlertSeverity;
}

