import { useQuery } from '@tanstack/react-query';
import { dashboardService } from '@/features/dashboard/dashboardService';
import { StatCard } from '@/components/ui/StatCard';
import { PageSpinner } from '@/components/ui/Spinner';
import { ErrorMessage, EmptyState } from '@/components/ui/ErrorMessage';
import { Badge } from '@/components/ui/Badge';
import { formatCurrency, formatNumber, startOfMonth, today } from '@/lib/utils';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts';

// ─── Query keys ───────────────────────────────────────────────────────────────
const from = startOfMonth();
const to   = today();

export function DashboardPage() {
  const summaryQ   = useQuery({ queryKey: ['dashboard', 'summary', from, to],   queryFn: () => dashboardService.getSummary({ from, to }),   staleTime: 60_000 });
  const trendQ     = useQuery({ queryKey: ['dashboard', 'trend', from, to],     queryFn: () => dashboardService.getSalesTrend({ from, to }), staleTime: 60_000 });
  const topItemsQ  = useQuery({ queryKey: ['dashboard', 'top-items', from, to], queryFn: () => dashboardService.getTopItems({ from, to }),   staleTime: 60_000 });
  const expiryQ    = useQuery({ queryKey: ['dashboard', 'expiry'],               queryFn: () => dashboardService.getExpiryRisk({ withinDays: 30 }), staleTime: 60_000 });

  const isLoading = summaryQ.isLoading || trendQ.isLoading;

  if (isLoading) return <PageSpinner />;

  const s = summaryQ.data;

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold text-gray-900">Dashboard</h1>

      {summaryQ.error && <ErrorMessage error={summaryQ.error} retry={summaryQ.refetch} />}

      {/* ── Summary cards ─────────────────────────────────────────────── */}
      {s && (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <StatCard title="Revenue (MTD)"      value={formatCurrency(s.sales.totalRevenue)}       icon="💰" />
          <StatCard title="Sales Invoices"     value={formatNumber(s.sales.invoiceCount)}          icon="🧾" />
          <StatCard title="Purchase Cost"      value={formatCurrency(s.purchases.totalCost)}       icon="🛒" />
          <StatCard title="Balance Due"        value={formatCurrency(s.purchases.balanceDue)}      icon="⏳" variant={s.purchases.balanceDue > 0 ? 'warning' : 'default'} />
          <StatCard title="Items Tracked"      value={formatNumber(s.inventory.distinctItems)}     icon="📦" />
          <StatCard title="Low Stock"          value={formatNumber(s.inventory.lowStockItemCount)} icon="⚠" variant={s.inventory.lowStockItemCount > 0 ? 'warning' : 'default'} />
          <StatCard title="Out of Stock"       value={formatNumber(s.inventory.outOfStockItemCount)} icon="🚫" variant={s.inventory.outOfStockItemCount > 0 ? 'danger' : 'default'} />
          <StatCard title="Active Alerts"      value={formatNumber(s.alerts.totalActive)}          icon="🔔" variant={s.alerts.totalActive > 0 ? 'danger' : 'default'} />
        </div>
      )}

      {/* ── Sales trend chart ─────────────────────────────────────────── */}
      <div className="card p-5">
        <h2 className="mb-4 text-sm font-semibold text-gray-700">Sales Trend (Last 30 Days)</h2>
        {trendQ.error && <ErrorMessage error={trendQ.error} />}
        {trendQ.data && trendQ.data.length === 0 && <EmptyState message="No sales data for this period." />}
        {trendQ.data && trendQ.data.length > 0 && (
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={trendQ.data} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
              <defs>
                <linearGradient id="revenueGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%"  stopColor="#3b82f6" stopOpacity={0.15} />
                  <stop offset="95%" stopColor="#3b82f6" stopOpacity={0}    />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
              <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false} tickFormatter={(v) => `₪${v.toLocaleString()}`} />
              <Tooltip formatter={(v: number) => formatCurrency(v)} />
              <Area type="monotone" dataKey="revenue" stroke="#3b82f6" strokeWidth={2} fill="url(#revenueGrad)" />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        {/* ── Top selling items ─────────────────────────────────────── */}
        <div className="card p-5">
          <h2 className="mb-4 text-sm font-semibold text-gray-700">Top Selling Items (MTD)</h2>
          {topItemsQ.error && <ErrorMessage error={topItemsQ.error} />}
          {topItemsQ.data && topItemsQ.data.length === 0 && <EmptyState message="No sales data." />}
          {topItemsQ.data && topItemsQ.data.length > 0 && (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 text-xs text-gray-500 uppercase">
                  <th className="pb-2 text-left font-medium">Item</th>
                  <th className="pb-2 text-right font-medium">Qty Sold</th>
                  <th className="pb-2 text-right font-medium">Revenue</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {topItemsQ.data.map((item) => (
                  <tr key={item.itemId} className="hover:bg-gray-50">
                    <td className="py-2">
                      <p className="font-medium text-gray-900">{item.itemName}</p>
                      <p className="text-xs text-gray-400">{item.itemCode}</p>
                    </td>
                    <td className="py-2 text-right tabular-nums text-gray-700">{formatNumber(item.quantitySold)}</td>
                    <td className="py-2 text-right tabular-nums text-gray-700">{formatCurrency(item.revenue)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* ── Expiry risk ───────────────────────────────────────────── */}
        <div className="card p-5">
          <h2 className="mb-4 text-sm font-semibold text-gray-700">Expiry Risk (Next 30 Days)</h2>
          {expiryQ.error && <ErrorMessage error={expiryQ.error} />}
          {expiryQ.data && expiryQ.data.length === 0 && <EmptyState message="No expiry risk items." />}
          {expiryQ.data && expiryQ.data.length > 0 && (
            <div className="space-y-2 max-h-64 overflow-y-auto">
              {expiryQ.data.map((batch) => (
                <div key={batch.batchId} className="flex items-start justify-between rounded-md border border-gray-100 bg-gray-50 px-3 py-2">
                  <div>
                    <p className="text-sm font-medium text-gray-900">{batch.itemName}</p>
                    <p className="text-xs text-gray-500">Batch {batch.batchNumber} · {batch.warehouseName}</p>
                    <p className="text-xs text-gray-400">Expires {batch.expiryDate} · Qty {formatNumber(batch.availableQuantity)}</p>
                  </div>
                  <Badge variant={batch.isExpired ? 'danger' : 'warning'}>
                    {batch.isExpired ? 'Expired' : `${batch.daysUntilExpiry}d`}
                  </Badge>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

