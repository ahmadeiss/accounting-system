import { api } from '@/lib/api';
import type { ItemDto, ItemsPage } from '@/types/items';
import type { CreateSaleRequest } from '@/types/sales';

// ─── Item search (reuses the existing /v1/items endpoint) ─────────────────────
// The backend already filters by name, SKU, and barcode in a single query.
// Barcode scanners enter text + Enter; the same search field handles both paths.

export async function searchItems(term: string): Promise<ItemDto[]> {
  if (!term.trim()) return [];
  const { data } = await api.get<ItemsPage>('/v1/items', {
    params: { search: term.trim(), isActive: true, page: 1, pageSize: 10 },
  });
  return data.items;
}

// ─── Exact barcode lookup ─────────────────────────────────────────────────────
// Same endpoint, page-size=1; the backend Contains() check is not exact but
// uniqueness is enforced at DB level (unique index on barcode), so a barcode
// search that returns one item is always the correct item.

export async function lookupBarcode(barcode: string): Promise<ItemDto | null> {
  const items = await searchItems(barcode);
  // Accept the result only if the barcode matches exactly (case-insensitive)
  const exact = items.find(
    (i) => i.barcode?.toLowerCase() === barcode.toLowerCase()
  );
  return exact ?? null;
}

// ─── Sales invoice create + confirm ──────────────────────────────────────────
// POS flow: create Draft (stock not touched) → confirm (FEFO deduction).
// Both calls happen in sequence inside a single mutation so the cashier
// sees one atomic action. The draft ID is never surfaced to the user.

export async function createSale(request: CreateSaleRequest): Promise<string> {
  const { data } = await api.post<{ id: string }>('/v1/sales-invoices', request);
  return data.id;
}

export async function confirmSale(id: string): Promise<void> {
  await api.post(`/v1/sales-invoices/${id}/confirm`);
}

