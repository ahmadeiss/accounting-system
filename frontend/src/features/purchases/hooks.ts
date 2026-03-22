import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  purchasesService,
  suppliersService,
  warehousesService,
} from './purchasesService';
import type { CreatePurchaseInvoiceRequest, PurchasesQuery } from '@/types/purchases';

// ─── Query keys ───────────────────────────────────────────────────────────────

export const purchaseKeys = {
  all:       ['purchases'] as const,
  list:      (q: PurchasesQuery) => ['purchases', 'list', q] as const,
  detail:    (id: string)        => ['purchases', 'detail', id] as const,
  suppliers: ['suppliers'] as const,
  warehouses:['warehouses'] as const,
};

// ─── Queries ──────────────────────────────────────────────────────────────────

export function usePurchases(query: PurchasesQuery) {
  return useQuery({
    queryKey: purchaseKeys.list(query),
    queryFn:  () => purchasesService.list(query),
    placeholderData: (prev) => prev,
  });
}

export function usePurchase(id: string) {
  return useQuery({
    queryKey: purchaseKeys.detail(id),
    queryFn:  () => purchasesService.getById(id),
    enabled:  !!id,
  });
}

export function useSuppliers() {
  return useQuery({
    queryKey: purchaseKeys.suppliers,
    queryFn:  suppliersService.list,
    staleTime: 5 * 60 * 1000,
  });
}

export function useWarehouses() {
  return useQuery({
    queryKey: purchaseKeys.warehouses,
    queryFn:  warehousesService.list,
    staleTime: 5 * 60 * 1000,
  });
}

// ─── Mutations ────────────────────────────────────────────────────────────────

export function useCreatePurchase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: CreatePurchaseInvoiceRequest) => purchasesService.create(req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: purchaseKeys.all });
    },
  });
}

export function useConfirmPurchase(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => purchasesService.confirm(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: purchaseKeys.detail(id) });
      qc.invalidateQueries({ queryKey: purchaseKeys.all });
    },
  });
}

