import { api } from '@/lib/api';
import type {
  DashboardSummary,
  DailySales,
  TopSellingItem,
  ExpiryRisk,
} from '@/types/dashboard';

// ─── Dashboard API service ────────────────────────────────────────────────────
// All endpoints: GET /api/v1/dashboard/*
// Date params use DateOnly format: "YYYY-MM-DD"

interface PeriodParams {
  from?: string;
  to?: string;
  branchId?: string;
  warehouseId?: string;
}

export const dashboardService = {
  async getSummary(params: PeriodParams = {}): Promise<DashboardSummary> {
    const { data } = await api.get<DashboardSummary>('/v1/dashboard/summary', { params });
    return data;
  },

  async getSalesTrend(params: PeriodParams = {}): Promise<DailySales[]> {
    const { data } = await api.get<DailySales[]>('/v1/dashboard/sales-trend', { params });
    return data;
  },

  async getTopItems(params: PeriodParams & { top?: number } = {}): Promise<TopSellingItem[]> {
    const { data } = await api.get<TopSellingItem[]>('/v1/dashboard/top-items', { params });
    return data;
  },

  async getExpiryRisk(params: { withinDays?: number; warehouseId?: string } = {}): Promise<ExpiryRisk[]> {
    const { data } = await api.get<ExpiryRisk[]>('/v1/dashboard/expiry-risk', { params });
    return data;
  },
};

