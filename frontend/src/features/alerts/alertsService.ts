import { api } from '@/lib/api';
import type { Alert, AlertFilters } from '@/types/alerts';

// ─── Alerts API service ───────────────────────────────────────────────────────
// GET  /api/v1/alerts
// POST /api/v1/alerts/{id}/acknowledge
// POST /api/v1/alerts/{id}/resolve

export const alertsService = {
  async getAlerts(filters: AlertFilters = {}): Promise<Alert[]> {
    const params: Record<string, string> = {};
    if (filters.type)     params.type     = filters.type;
    if (filters.status)   params.status   = filters.status;
    if (filters.severity) params.severity = filters.severity;

    const { data } = await api.get<Alert[]>('/v1/alerts', { params });
    return data;
  },

  async acknowledge(id: string): Promise<void> {
    await api.post(`/v1/alerts/${id}/acknowledge`);
  },

  async resolve(id: string): Promise<void> {
    await api.post(`/v1/alerts/${id}/resolve`);
  },
};

