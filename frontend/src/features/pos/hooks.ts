import { useMutation, useQuery } from '@tanstack/react-query';
import { useReducer } from 'react';
import { searchItems, createSale, confirmSale } from './posService';
import type { CartAction, CartLine, CreateSaleRequest } from '@/types/sales';

// ─── Cart reducer ─────────────────────────────────────────────────────────────
// All cart mutations go through this reducer — stable dispatch reference
// prevents child re-renders from parent state changes.

export function cartReducer(state: CartLine[], action: CartAction): CartLine[] {
  switch (action.type) {
    case 'ADD_OR_INCREMENT': {
      const idx = state.findIndex((l) => l.itemId === action.item.id);
      if (idx >= 0) {
        return state.map((l, i) =>
          i === idx ? { ...l, quantity: l.quantity + 1 } : l
        );
      }
      const newLine: CartLine = {
        itemId:          action.item.id,
        itemName:        action.item.name,
        itemSKU:         action.item.sku,
        unitPrice:       action.item.salePrice,
        quantity:        1,
        discountPercent: 0,
        taxPercent:      0,
      };
      return [...state, newLine];
    }

    case 'INC_QTY':
      return state.map((l) =>
        l.itemId === action.itemId ? { ...l, quantity: l.quantity + 1 } : l
      );

    case 'DEC_QTY':
      return state
        .map((l) =>
          l.itemId === action.itemId ? { ...l, quantity: l.quantity - 1 } : l
        )
        .filter((l) => l.quantity > 0);

    case 'SET_QTY': {
      if (action.qty < 1) return state.filter((l) => l.itemId !== action.itemId);
      return state.map((l) =>
        l.itemId === action.itemId ? { ...l, quantity: action.qty } : l
      );
    }

    case 'SET_PRICE':
      return state.map((l) =>
        l.itemId === action.itemId
          ? { ...l, unitPrice: Math.max(0, action.price) }
          : l
      );

    case 'REMOVE':
      return state.filter((l) => l.itemId !== action.itemId);

    case 'CLEAR':
      return [];

    default:
      return state;
  }
}

// ─── useCart ──────────────────────────────────────────────────────────────────

export function useCart() {
  const [lines, dispatch] = useReducer(cartReducer, []);

  // Computed values kept here to avoid recalculation in multiple children
  const subTotal = lines.reduce(
    (sum, l) => sum + l.quantity * l.unitPrice * (1 - l.discountPercent / 100),
    0
  );
  const taxAmount = lines.reduce(
    (sum, l) =>
      sum + l.quantity * l.unitPrice * (1 - l.discountPercent / 100) * (l.taxPercent / 100),
    0
  );
  const total = subTotal + taxAmount;

  return { lines, dispatch, subTotal, taxAmount, total };
}

// ─── useItemSearch ────────────────────────────────────────────────────────────
// Enabled only when the search term is non-empty.
// staleTime=10s avoids hammering the server on rapid keystrokes.

export function useItemSearch(debouncedTerm: string) {
  return useQuery({
    queryKey: ['pos', 'search', debouncedTerm],
    queryFn:  () => searchItems(debouncedTerm),
    enabled:  debouncedTerm.trim().length >= 1,
    staleTime: 10_000,
    placeholderData: (prev) => prev,
  });
}

// ─── useConfirmSale ───────────────────────────────────────────────────────────
// Single mutation: create draft → confirm.
// Returns the invoice id on success.
// On any error the draft may be orphaned — acceptable for MVP (drafts expire).

export function useConfirmSale() {
  return useMutation({
    mutationFn: async (request: CreateSaleRequest): Promise<string> => {
      const id = await createSale(request);
      await confirmSale(id);
      return id;
    },
  });
}

