import { useParams, Link } from 'react-router-dom';
import { usePurchase, useConfirmPurchase } from '@/features/purchases/hooks';
import { useAuthStore } from '@/features/auth/authStore';
import { Badge } from '@/components/ui/Badge';
import { PageSpinner } from '@/components/ui/Spinner';
import { ErrorMessage } from '@/components/ui/ErrorMessage';
import { formatCurrency } from '@/lib/utils';
import { extractErrorMessage } from '@/lib/utils';
import { useState } from 'react';
import type { PurchaseStatus } from '@/types/purchases';

const statusVariant = (s: PurchaseStatus) =>
  s === 'Confirmed' ? 'success' : s === 'Cancelled' ? 'danger' : 'warning';

export default function PurchaseDetailPage() {
  const { id }         = useParams<{ id: string }>();
  const { hasPermission } = useAuthStore();
  const canConfirm     = hasPermission('purchases.confirm');

  const { data: inv, isLoading, isError, error } = usePurchase(id!);
  const confirm = useConfirmPurchase(id!);
  const [confirmError, setConfirmError] = useState<unknown>(null);

  if (isLoading) return <PageSpinner />;
  if (isError)   return <ErrorMessage error={error} />;
  if (!inv)      return null;

  const isDraft     = inv.status === 'Draft';
  const isConfirmed = inv.status === 'Confirmed';

  async function handleConfirm() {
    setConfirmError(null);
    try {
      await confirm.mutateAsync();
    } catch (err) {
      setConfirmError(err);
    }
  }

  return (
    <div className="space-y-6 max-w-5xl">
      {/* ── Header bar ── */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-semibold text-gray-900">{inv.invoiceNumber}</h1>
            <Badge variant={statusVariant(inv.status)}>{inv.status}</Badge>
            {isConfirmed && (
              <span className="text-xs text-gray-500 bg-gray-100 rounded px-2 py-0.5">
                🔒 Read-only
              </span>
            )}
          </div>
          <p className="text-sm text-gray-500 mt-0.5">
            Created by {inv.createdByName} · {new Date(inv.createdAt).toLocaleDateString()}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Link to="/purchases" className="text-sm text-gray-500 hover:underline">
            ← Back
          </Link>
          {isDraft && canConfirm && (
            <button
              onClick={handleConfirm}
              disabled={confirm.isPending}
              className="rounded bg-emerald-600 px-4 py-1.5 text-sm text-white hover:bg-emerald-700 disabled:opacity-50"
            >
              {confirm.isPending ? 'Confirming…' : 'Confirm Invoice'}
            </button>
          )}
        </div>
      </div>

      {!!confirmError && (
        <ErrorMessage error={confirmError} title={extractErrorMessage(confirmError)} />
      )}

      {/* ── Meta grid ── */}
      <div className="rounded border border-gray-200 bg-white shadow-sm">
        <dl className="grid grid-cols-2 gap-0 divide-x divide-y divide-gray-100 sm:grid-cols-4">
          {[
            { label: 'Supplier',  value: inv.supplierName  },
            { label: 'Warehouse', value: inv.warehouseName },
            { label: 'Invoice Date', value: inv.invoiceDate },
            { label: 'Due Date',  value: inv.dueDate ?? '—' },
          ].map((f) => (
            <div key={f.label} className="px-4 py-3">
              <dt className="text-xs text-gray-500">{f.label}</dt>
              <dd className="mt-0.5 text-sm font-medium text-gray-800">{f.value}</dd>
            </div>
          ))}
        </dl>
        {inv.notes && (
          <div className="border-t border-gray-100 px-4 py-3">
            <dt className="text-xs text-gray-500">Notes</dt>
            <dd className="mt-0.5 text-sm text-gray-700">{inv.notes}</dd>
          </div>
        )}
      </div>

      {/* ── Lines table ── */}
      <div className="overflow-x-auto rounded border border-gray-200 bg-white shadow-sm">
        <table className="min-w-full divide-y divide-gray-100 text-sm">
          <thead className="bg-gray-50 text-xs font-medium uppercase text-gray-500">
            <tr>
              {['Item', 'SKU', 'Qty', 'Unit Cost', 'Disc%', 'Tax%', 'Total', 'Batch', 'Expiry'].map((h) => (
                <th key={h} className="px-4 py-3 text-left">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {inv.lines.map((line) => (
              <tr key={line.id}>
                <td className="px-4 py-2">{line.itemName}</td>
                <td className="px-4 py-2 font-mono text-xs text-gray-500">{line.itemSKU}</td>
                <td className="px-4 py-2 text-right">{line.quantity}</td>
                <td className="px-4 py-2 text-right">{formatCurrency(line.unitCost)}</td>
                <td className="px-4 py-2 text-right">{line.discountPercent}%</td>
                <td className="px-4 py-2 text-right">{line.taxPercent}%</td>
                <td className="px-4 py-2 text-right font-medium">{formatCurrency(line.lineTotal)}</td>
                <td className="px-4 py-2 text-xs text-gray-500">{line.batchNumber ?? '—'}</td>
                <td className="px-4 py-2 text-xs text-gray-500">{line.expiryDate ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* ── Totals ── */}
      <div className="flex justify-end">
        <dl className="w-64 space-y-1 text-sm">
          {[
            { label: 'Sub-total',  value: formatCurrency(inv.subTotal)       },
            { label: 'Discount',   value: `- ${formatCurrency(inv.discountAmount)}` },
            { label: 'Tax',        value: `+ ${formatCurrency(inv.taxAmount)}`      },
          ].map((r) => (
            <div key={r.label} className="flex justify-between text-gray-600">
              <dt>{r.label}</dt><dd>{r.value}</dd>
            </div>
          ))}
          <div className="flex justify-between border-t pt-1 font-semibold text-gray-900">
            <dt>Total</dt><dd>{formatCurrency(inv.totalAmount)}</dd>
          </div>
          {inv.balanceDue > 0 && (
            <div className="flex justify-between text-red-600">
              <dt>Balance Due</dt><dd>{formatCurrency(inv.balanceDue)}</dd>
            </div>
          )}
        </dl>
      </div>
    </div>
  );
}

