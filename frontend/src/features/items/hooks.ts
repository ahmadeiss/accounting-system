import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { categoriesService, itemsService, unitsService } from './itemsService';
import type { CreateItemRequest, ItemsQuery, UpdateItemRequest } from '@/types/items';

// ─── Query keys ───────────────────────────────────────────────────────────────

export const itemKeys = {
  all:    ['items'] as const,
  list:   (q: ItemsQuery) => ['items', 'list', q] as const,
  detail: (id: string)    => ['items', 'detail', id] as const,
};

export const lookupKeys = {
  categories: ['categories'] as const,
  units:      ['units'] as const,
};

// ─── Queries ──────────────────────────────────────────────────────────────────

export function useItems(query: ItemsQuery) {
  return useQuery({
    queryKey: itemKeys.list(query),
    queryFn:  () => itemsService.list(query),
    placeholderData: (prev) => prev,
  });
}

export function useItem(id: string) {
  return useQuery({
    queryKey: itemKeys.detail(id),
    queryFn:  () => itemsService.getById(id),
    enabled:  !!id,
  });
}

export function useCategories() {
  return useQuery({
    queryKey: lookupKeys.categories,
    queryFn:  categoriesService.list,
    staleTime: 5 * 60 * 1000, // 5 min — lookups change rarely
  });
}

export function useUnits() {
  return useQuery({
    queryKey: lookupKeys.units,
    queryFn:  unitsService.list,
    staleTime: 5 * 60 * 1000,
  });
}

// ─── Mutations ────────────────────────────────────────────────────────────────

export function useCreateItem() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateItemRequest) => itemsService.create(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: itemKeys.all }),
  });
}

export function useUpdateItem(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdateItemRequest) => itemsService.update(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: itemKeys.all });
      qc.invalidateQueries({ queryKey: itemKeys.detail(id) });
    },
  });
}

export function useToggleItemActive() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => itemsService.toggleActive(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: itemKeys.all }),
  });
}

