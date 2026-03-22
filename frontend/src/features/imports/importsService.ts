import { api } from '@/lib/api';
import type { ImportResult } from '@/types/imports';

const BASE = '/v1/imports';

export interface ImportItemsParams {
  file:   File;
  dryRun: boolean;
}

export interface ImportOpeningStockParams {
  file:        File;
  warehouseId: string;
  dryRun:      boolean;
}

export const importsService = {
  /** Upload an .xlsx file for item master import. */
  importItems: async ({ file, dryRun }: ImportItemsParams): Promise<ImportResult> => {
    const form = new FormData();
    form.append('file', file);
    const { data } = await api.post<ImportResult>(`${BASE}/items`, form, {
      params: { dryRun },
      headers: { 'Content-Type': 'multipart/form-data' },
      validateStatus: (s) => s < 500,
    });
    return data;
  },

  /**
   * Upload an .xlsx for opening-stock import.
   * Columns: SKU, Quantity, CostPerUnit, BatchNumber, ExpiryDate, ProductionDate.
   * warehouseId is sent as a query param.
   */
  importOpeningStock: async ({
    file,
    warehouseId,
    dryRun,
  }: ImportOpeningStockParams): Promise<ImportResult> => {
    const form = new FormData();
    form.append('file', file);
    const { data } = await api.post<ImportResult>(`${BASE}/opening-stock`, form, {
      params: { warehouseId, dryRun },
      headers: { 'Content-Type': 'multipart/form-data' },
      validateStatus: (s) => s < 500,
    });
    return data;
  },

  /** Retrieve a previously executed import job result. */
  getJobResult: async (jobId: string): Promise<ImportResult> => {
    const { data } = await api.get<ImportResult>(`${BASE}/${jobId}`);
    return data;
  },
};

