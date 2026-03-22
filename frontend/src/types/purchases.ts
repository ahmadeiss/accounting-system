// ─── List DTO (lightweight, no lines) ────────────────────────────────────────

export interface PurchaseInvoiceListDto {
  id:            string;
  invoiceNumber: string;
  supplierName:  string;
  warehouseName: string;
  invoiceDate:   string; // "YYYY-MM-DD"
  dueDate:       string | null;
  status:        PurchaseStatus;
  lineCount:     number;
  totalAmount:   number;
  createdAt:     string;
}

export interface PurchaseInvoicesPage {
  total:    number;
  page:     number;
  pageSize: number;
  items:    PurchaseInvoiceListDto[];
}

// ─── Detail DTO (full, with lines) ───────────────────────────────────────────

export interface PurchaseInvoiceDto {
  id:             string;
  invoiceNumber:  string;
  supplierName:   string;
  supplierId:     string;
  branchName:     string;
  warehouseName:  string;
  invoiceDate:    string;
  dueDate:        string | null;
  status:         PurchaseStatus;
  subTotal:       number;
  taxAmount:      number;
  discountAmount: number;
  totalAmount:    number;
  paidAmount:     number;
  balanceDue:     number;
  notes:          string | null;
  createdByName:  string;
  createdAt:      string;
  lines:          PurchaseInvoiceLineDto[];
}

export interface PurchaseInvoiceLineDto {
  id:              string;
  itemId:          string;
  itemName:        string;
  itemSKU:         string;
  quantity:        number;
  unitCost:        number;
  discountPercent: number;
  taxPercent:      number;
  lineTotal:       number;
  batchNumber:     string | null;
  productionDate:  string | null;
  expiryDate:      string | null;
  notes:           string | null;
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

export interface CreatePurchaseInvoiceLineRequest {
  itemId:          string;
  quantity:        number;
  unitCost:        number;
  discountPercent: number;
  taxPercent:      number;
  batchNumber?:    string;
  productionDate?: string; // "YYYY-MM-DD"
  expiryDate?:     string; // "YYYY-MM-DD"
  notes?:          string;
}

export interface CreatePurchaseInvoiceRequest {
  supplierId:  string;
  branchId:    string;
  warehouseId: string;
  invoiceDate: string; // "YYYY-MM-DD"
  dueDate?:    string;
  notes?:      string;
  lines:       CreatePurchaseInvoiceLineRequest[];
}

// ─── Query / filter ───────────────────────────────────────────────────────────

export interface PurchasesQuery {
  page:        number;
  pageSize:    number;
  status?:     string;
  supplierId?: string;
  warehouseId?: string;
}

// ─── Enums ────────────────────────────────────────────────────────────────────

export type PurchaseStatus = 'Draft' | 'Confirmed' | 'Cancelled';

// ─── Lookup types ─────────────────────────────────────────────────────────────

export interface SupplierLookup {
  id:   string;
  name: string;
  code: string;
}

export interface WarehouseLookup {
  id:         string;
  name:       string;
  code:       string;
  isDefault:  boolean;
  branchId:   string;
  branchName: string;
}

