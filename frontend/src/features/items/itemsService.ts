import { api } from '@/lib/api';
import type {
  CategoryLookup,
  CreateItemRequest,
  ItemDto,
  ItemsPage,
  ItemsQuery,
  UnitLookup,
  UpdateItemRequest,
} from '@/types/items';

const BASE = '/v1/items';

export const itemsService = {
  /** Paginated, filtered list. */
  list: async (params: ItemsQuery): Promise<ItemsPage> => {
    const { data } = await api.get<ItemsPage>(BASE, { params });
    return data;
  },

  /** Single item by ID. */
  getById: async (id: string): Promise<ItemDto> => {
    const { data } = await api.get<ItemDto>(`${BASE}/${id}`);
    return data;
  },

  /** Create a new item. */
  create: async (body: CreateItemRequest): Promise<{ id: string }> => {
    const { data } = await api.post<{ id: string }>(BASE, body);
    return data;
  },

  /** Full update of an existing item. */
  update: async (id: string, body: UpdateItemRequest): Promise<void> => {
    await api.put(`${BASE}/${id}`, body);
  },

  /** Toggle active/inactive. */
  toggleActive: async (id: string): Promise<{ id: string; isActive: boolean }> => {
    const { data } = await api.patch<{ id: string; isActive: boolean }>(
      `${BASE}/${id}/toggle-active`
    );
    return data;
  },
};

export const categoriesService = {
  list: async (): Promise<CategoryLookup[]> => {
    const { data } = await api.get<CategoryLookup[]>('/v1/categories');
    return data;
  },
};

export const unitsService = {
  list: async (): Promise<UnitLookup[]> => {
    const { data } = await api.get<UnitLookup[]>('/v1/units');
    return data;
  },
};

