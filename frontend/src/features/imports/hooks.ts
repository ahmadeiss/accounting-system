import { useMutation, useQuery } from '@tanstack/react-query';
import {
  importsService,
  type ImportItemsParams,
  type ImportOpeningStockParams,
} from './importsService';

export function useImportItems() {
  return useMutation({
    mutationFn: (params: ImportItemsParams) => importsService.importItems(params),
  });
}

export function useImportOpeningStock() {
  return useMutation({
    mutationFn: (params: ImportOpeningStockParams) =>
      importsService.importOpeningStock(params),
  });
}

export function useImportJobResult(jobId: string | null) {
  return useQuery({
    queryKey: ['import-job', jobId],
    queryFn:  () => importsService.getJobResult(jobId!),
    enabled:  !!jobId,
  });
}

