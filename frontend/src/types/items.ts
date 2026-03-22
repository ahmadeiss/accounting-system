// ─── Lookup types ─────────────────────────────────────────────────────────────

export interface CategoryLookup {
  id: string;
  name: string;
}

export interface UnitLookup {
  id: string;
  name: string;
  abbreviation: string;
}

// ─── Item DTO (mirrors backend ItemDto) ───────────────────────────────────────

export interface ItemDto {
  id: string;
  name: string;
  sku: string;
  barcode: string | null;
  description: string | null;
  categoryName: string;
  categoryId: string;
  unitName: string;
  unitAbbreviation: string;
  unitId: string;
  costPrice: number;
  salePrice: number;
  reorderLevel: number;
  trackBatch: boolean;
  trackExpiry: boolean;
  minExpiryDaysBeforeSale: number;
  isActive: boolean;
  createdAt: string;
}

// ─── Paginated response ───────────────────────────────────────────────────────

export interface ItemsPage {
  items: ItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ─── Query params ─────────────────────────────────────────────────────────────

export interface ItemsQuery {
  page: number;
  pageSize: number;
  search?: string;
  isActive?: boolean;
}

// ─── Create / Update request bodies ──────────────────────────────────────────

export interface CreateItemRequest {
  name: string;
  sku: string;
  barcode?: string;
  description?: string;
  categoryId: string;
  unitId: string;
  costPrice: number;
  salePrice: number;
  reorderLevel: number;
  trackBatch: boolean;
  trackExpiry: boolean;
  minExpiryDaysBeforeSale: number;
}

export interface UpdateItemRequest {
  name: string;
  barcode?: string;
  description?: string;
  categoryId: string;
  unitId: string;
  costPrice: number;
  salePrice: number;
  reorderLevel: number;
  trackBatch: boolean;
  trackExpiry: boolean;
  minExpiryDaysBeforeSale: number;
  isActive: boolean;
}

