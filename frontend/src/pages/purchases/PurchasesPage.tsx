import { useState } from 'react';
import { Link } from 'react-router-dom';
import { usePurchases, useConfirmPurchase, useSuppliers, useWarehouses } from '@/features/purchases/hooks';
import { useAuthStore } from '@/features/auth/authStore';
import { Badge } from '@/components/ui/Badge';
import { PageSpinner } from '@/components/ui/Spinner';
import { ErrorMessage } from '@/components/ui/ErrorMessage';
import { formatCurrency } from '@/lib/utils';
import type { PurchasesQuery, PurchaseStatus } from '@/types/purchases';

const STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: '',           label: 'All statuses' },
  { value: 'Draft',     label: 'Draft'         },
  { value: 'Confirmed', label: 'Confirmed'      },
  { value: 'Cancelled', label: 'Cancelled'      },
];

const statusVariant = (s: PurchaseStatus) =>
  s === 'Confirmed' ? 'success' : s === 'Cancelled' ? 'danger' : 'warning';

// ─── Confirm button — isolated so mutation state is per-row ──────────────────

function ConfirmButton({ id }: { id: string }) {
  const { mutate, isPending } = useConfirmPurchase(id);
  return (
    <button
      onClick={() => mutate()}
      disabled={isPending}
      className="text-xs text-emerald-700 hover:underline disabled:opacity-50"
    >
      {isPending ? 'Confirming…' : 'Confirm'}
    </button>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function PurchasesPage() {
  const { hasPermission } = useAuthStore();
  const canCreate  = hasPermission('purchases.read');    // same gate as create
  const canConfirm = hasPermission('purchases.confirm');

  const [query, setQuery] = useState<PurchasesQuery>({
    page: 1, pageSize: 20,
  });

  const { data, isLoading, isError, error } = usePurchases(query);
  const { data: suppliers  } = useSuppliers();
  const { data: warehouses } = useWarehouses();

  const set = (patch: Partial<PurchasesQuery>) =>
    setQuery((q) => ({ ...q, ...patch, page: 1 }));

  if (isLoading) return <PageSpinner />;
  if (isError)   return <ErrorMessage error={error} />;

  return (
    <div className="space-y-4">
      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-gray-900">Purchase Invoices</h1>
        {canCreate && (
          <Link
            to="/purchases/new"
            className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700"
          >
            + New Invoice
          </Link>
        )}
      </div>

      {/* ── Filters ── */}
      <div className="flex flex-wrap gap-3">
        <select
          value={query.status ?? ''}
          onChange={(e) => set({ status: e.target.value || undefined })}
          className="rounded border border-gray-300 px-2 py-1.5 text-sm"
        >
          {STATUS_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>

        <select
          value={query.supplierId ?? ''}
          onChange={(e) => set({ supplierId: e.target.value || undefined })}
          className="rounded border border-gray-300 px-2 py-1.5 text-sm"
        >
          <option value="">All suppliers</option>
          {suppliers?.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>

        <select
          value={query.warehouseId ?? ''}
          onChange={(e) => set({ warehouseId: e.target.value || undefined })}
          className="rounded border border-gray-300 px-2 py-1.5 text-sm"
        >
          <option value="">All warehouses</option>
          {warehouses?.map((w) => (
            <option key={w.id} value={w.id}>{w.name}</option>
          ))}
        </select>
      </div>

      {/* ── Table ── */}
      <div className="overflow-x-auto rounded border border-gray-200 bg-white shadow-sm">
        <table className="min-w-full divide-y divide-gray-100 text-sm">
          <thead className="bg-gray-50 text-xs font-medium uppercase text-gray-500">
            <tr>
              {['Invoice #', 'Supplier', 'Warehouse', 'Date', 'Lines', 'Total', 'Status', 'Actions'].map((h) => (
                <th key={h} className="px-4 py-3 text-left">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {data?.items.length === 0 && (
              <tr>
                <td colSpan={8} className="px-4 py-8 text-center text-gray-400">
                  No purchase invoices found.
                </td>
              </tr>
            )}
            {data?.items.map((inv) => (
              <tr key={inv.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 font-mono text-xs">{inv.invoiceNumber}</td>
                <td className="px-4 py-3">{inv.supplierName}</td>
                <td className="px-4 py-3 text-gray-500">{inv.warehouseName}</td>
                <td className="px-4 py-3 text-gray-500">{inv.invoiceDate}</td>
                <td className="px-4 py-3 text-center">{inv.lineCount}</td>
                <td className="px-4 py-3 text-right">{formatCurrency(inv.totalAmount)}</td>
                <td className="px-4 py-3">
                  <Badge variant={statusVariant(inv.status)}>{inv.status}</Badge>
                </td>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <Link to={`/purchases/${inv.id}`} className="text-xs text-blue-600 hover:underline">
                      View
                    </Link>
                    {canConfirm && inv.status === 'Draft' && (
                      <ConfirmButton id={inv.id} />
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* ── Pagination ── */}
      {data && data.total > query.pageSize && (
        <div className="flex items-center justify-between text-sm text-gray-500">
          <span>
            {(query.page - 1) * query.pageSize + 1}–
            {Math.min(query.page * query.pageSize, data.total)} of {data.total}
          </span>
          <div className="flex gap-2">
            <button
              disabled={query.page === 1}
              onClick={() => setQuery((q) => ({ ...q, page: q.page - 1 }))}
              className="rounded border px-2 py-1 disabled:opacity-40"
            >← Prev</button>
            <button
              disabled={query.page * query.pageSize >= data.total}
              onClick={() => setQuery((q) => ({ ...q, page: q.page + 1 }))}
              className="rounded border px-2 py-1 disabled:opacity-40"
            >Next →</button>
          </div>
        </div>
      )}
    </div>
  );
}

