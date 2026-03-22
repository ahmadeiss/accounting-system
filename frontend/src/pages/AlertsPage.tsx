import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { alertsService } from '@/features/alerts/alertsService';
import type { AlertFilters, AlertType, AlertStatus, AlertSeverity } from '@/types/alerts';
import { PageSpinner } from '@/components/ui/Spinner';
import { ErrorMessage, EmptyState } from '@/components/ui/ErrorMessage';
import { severityBadge, statusBadge } from '@/components/ui/Badge';
import { useAuthStore } from '@/features/auth/authStore';
import { formatDate } from '@/lib/utils';

const ALERT_TYPES:    AlertType[]    = ['LowStock', 'NearExpiry', 'ExpiredStock', 'BatchRecalled'];
const ALERT_STATUSES: AlertStatus[]  = ['Active', 'Acknowledged', 'Resolved'];
const SEVERITIES:     AlertSeverity[] = ['Info', 'Warning', 'Critical'];

export function AlertsPage() {
  const queryClient = useQueryClient();
  const canManage   = useAuthStore((s) => s.hasPermission('alerts.manage'));

  const [filters, setFilters] = useState<AlertFilters>({});

  const { data: alerts, isLoading, error, refetch } = useQuery({
    queryKey: ['alerts', filters],
    queryFn:  () => alertsService.getAlerts(filters),
    staleTime: 30_000,
  });

  const acknowledgeM = useMutation({
    mutationFn: alertsService.acknowledge,
    onSuccess:  () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  const resolveM = useMutation({
    mutationFn: alertsService.resolve,
    onSuccess:  () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  function setFilter<K extends keyof AlertFilters>(key: K, value: AlertFilters[K] | '') {
    setFilters((prev) => {
      const next = { ...prev };
      if (value === '') delete next[key];
      else (next[key] as AlertFilters[K]) = value as AlertFilters[K];
      return next;
    });
  }

  if (isLoading) return <PageSpinner />;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-gray-900">Alerts</h1>
        <span className="text-sm text-gray-500">{alerts?.length ?? 0} result{alerts?.length !== 1 ? 's' : ''}</span>
      </div>

      {/* ── Filters ───────────────────────────────────────────────────── */}
      <div className="card p-4 flex flex-wrap gap-3">
        <select
          className="input w-auto text-sm"
          value={filters.type ?? ''}
          onChange={(e) => setFilter('type', e.target.value as AlertType | '')}
        >
          <option value="">All types</option>
          {ALERT_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
        </select>

        <select
          className="input w-auto text-sm"
          value={filters.status ?? ''}
          onChange={(e) => setFilter('status', e.target.value as AlertStatus | '')}
        >
          <option value="">All statuses</option>
          {ALERT_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>

        <select
          className="input w-auto text-sm"
          value={filters.severity ?? ''}
          onChange={(e) => setFilter('severity', e.target.value as AlertSeverity | '')}
        >
          <option value="">All severities</option>
          {SEVERITIES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>

        <button className="btn-secondary text-sm" onClick={() => setFilters({})}>
          Clear
        </button>
      </div>

      {/* ── Errors ───────────────────────────────────────────────────── */}
      {error && <ErrorMessage error={error} retry={refetch} />}
      {(acknowledgeM.error) && <ErrorMessage error={acknowledgeM.error} title="Acknowledge failed" />}
      {(resolveM.error)     && <ErrorMessage error={resolveM.error}     title="Resolve failed"     />}

      {/* ── Table ────────────────────────────────────────────────────── */}
      {alerts && alerts.length === 0 && <EmptyState message="No alerts match the current filters." />}
      {alerts && alerts.length > 0 && (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b border-gray-200 bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Message</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Severity</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Created</th>
                {canManage && (
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {alerts.map((alert) => (
                <tr key={alert.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 font-medium text-gray-800 whitespace-nowrap">{alert.alertType}</td>
                  <td className="px-4 py-3 text-gray-600 max-w-sm">
                    <p>{alert.message}</p>
                    {alert.itemName  && <p className="text-xs text-gray-400">{alert.itemName} {alert.itemSku ? `(${alert.itemSku})` : ''}</p>}
                    {alert.warehouseName && <p className="text-xs text-gray-400">Warehouse: {alert.warehouseName}</p>}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap">{severityBadge(alert.severity)}</td>
                  <td className="px-4 py-3 whitespace-nowrap">{statusBadge(alert.status)}</td>
                  <td className="px-4 py-3 text-gray-500 whitespace-nowrap text-xs">{formatDate(alert.createdAt)}</td>
                  {canManage && (
                    <td className="px-4 py-3 whitespace-nowrap">
                      <div className="flex items-center gap-2">
                        {alert.status === 'Active' && (
                          <button
                            className="btn-secondary text-xs py-1 px-2"
                            disabled={acknowledgeM.isPending}
                            onClick={() => acknowledgeM.mutate(alert.id)}
                          >
                            Acknowledge
                          </button>
                        )}
                        {(alert.status === 'Active' || alert.status === 'Acknowledged') && (
                          <button
                            className="btn-primary text-xs py-1 px-2"
                            disabled={resolveM.isPending}
                            onClick={() => resolveM.mutate(alert.id)}
                          >
                            Resolve
                          </button>
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

