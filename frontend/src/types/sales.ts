// ─── Payment method (mirrors backend Accounting.Core.Enums.PaymentMethod) ─────

export type PaymentMethod = 1 | 2 | 3 | 4; // Cash | Card | BankTransfer | Mixed
export const PAYMENT_METHODS: { value: PaymentMethod; label: string }[] = [
  { value: 1, label: 'Cash'          },
  { value: 2, label: 'Card'          },
  { value: 3, label: 'Bank Transfer' },
];

// ─── POS session (set once per cashier shift) ─────────────────────────────────

export interface PosSession {
  warehouseId:   string;
  branchId:      string;
  warehouseName: string;
}

// ─── Cart (local component state — never sent to server as-is) ────────────────

export interface CartLine {
  itemId:          string;
  itemName:        string;
  itemSKU:         string;
  unitPrice:       number;
  quantity:        number;
  discountPercent: number; // 0 in MVP
  taxPercent:      number; // 0 in MVP
}

export type CartAction =
  | { type: 'ADD_OR_INCREMENT'; item: { id: string; name: string; sku: string; salePrice: number } }
  | { type: 'SET_QTY';   itemId: string; qty: number }
  | { type: 'INC_QTY';   itemId: string }
  | { type: 'DEC_QTY';   itemId: string }
  | { type: 'SET_PRICE'; itemId: string; price: number }
  | { type: 'REMOVE';    itemId: string }
  | { type: 'CLEAR' };

// ─── API request types (mirrors backend SalesDTOs.cs) ────────────────────────

export interface CreateSaleLineRequest {
  itemId:          string;
  quantity:        number;
  unitPrice:       number;
  discountPercent: number;
  taxPercent:      number;
}

export interface CreateSaleRequest {
  branchId:      string;
  warehouseId:   string;
  customerId:    string | null;
  paymentMethod: PaymentMethod;
  paidAmount:    number;
  notes:         string | null;
  lines:         CreateSaleLineRequest[];
}

// ─── Sale result ──────────────────────────────────────────────────────────────

export interface SaleResult {
  invoiceId: string;
  paidAmount:   number;
  changeAmount: number;
  totalAmount:  number;
}

