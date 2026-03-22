import { api } from '@/lib/api';
import type {
  PurchaseInvoicesPage,
  PurchaseInvoiceDto,
  CreatePurchaseInvoiceRequest,
  PurchasesQuery,
  SupplierLookup,
  WarehouseLookup,
} from '@/types/purchases';

const BASE = '/v1/purchase-invoices';

export const purchasesService = {
  list: async (q: PurchasesQuery): Promise<PurchaseInvoicesPage> => {
    const { data } = await api.get<PurchaseInvoicesPage>(BASE, {
      params: {
        page:        q.page,
        pageSize:    q.pageSize,
        status:      q.status      || undefined,
        supplierId:  q.supplierId  || undefined,
        warehouseId: q.warehouseId || undefined,
      },
    });
    return data;
  },

  getById: async (id: string): Promise<PurchaseInvoiceDto> => {
    const { data } = await api.get<PurchaseInvoiceDto>(`${BASE}/${id}`);
    return data;
  },

  create: async (request: CreatePurchaseInvoiceRequest): Promise<{ id: string }> => {
    const { data } = await api.post<{ id: string }>(BASE, request);
    return data;
  },

  confirm: async (id: string): Promise<void> => {
    await api.post(`${BASE}/${id}/confirm`);
  },
};

// ─── Lookup services ──────────────────────────────────────────────────────────

export const suppliersService = {
  list: async (): Promise<SupplierLookup[]> => {
    const { data } = await api.get<SupplierLookup[]>('/v1/suppliers');
    return data;
  },
};

export const warehousesService = {
  list: async (): Promise<WarehouseLookup[]> => {
    const { data } = await api.get<WarehouseLookup[]>('/v1/warehouses');
    return data;
  },
};

