import { useState } from 'react';
import { PAYMENT_METHODS, type PaymentMethod } from '@/types/sales';
import { extractErrorMessage } from '@/lib/utils';

interface PaymentPanelProps {
  subTotal:  number;
  taxAmount: number;
  total:     number;
  isEmpty:   boolean;
  isPending: boolean;
  error:     unknown;
  onConfirm: (paymentMethod: PaymentMethod, paidAmount: number) => void;
}

/**
 * Right-side payment panel.
 *
 * - Totals summary (sub-total, tax, grand total)
 * - Payment method selector (Cash / Card / Bank Transfer)
 * - Paid amount input (Cash only — shows change due)
 * - Confirm Sale button
 * - Error display (inline, no modal)
 */
export function PaymentPanel({
  subTotal,
  taxAmount,
  total,
  isEmpty,
  isPending,
  error,
  onConfirm,
}: PaymentPanelProps) {
  const [method, setMethod]         = useState<PaymentMethod>(1); // Cash default
  const [paidInput, setPaidInput]   = useState('');

  const paidAmount  = parseFloat(paidInput) || 0;
  const changeAmount = method === 1 ? Math.max(0, paidAmount - total) : 0;
  const shortfall    = method === 1 && paidAmount > 0 && paidAmount < total;

  const canConfirm =
    !isEmpty &&
    !isPending &&
    (method !== 1 || paidAmount >= total);

  function handleConfirm() {
    if (!canConfirm) return;
    onConfirm(method, method === 1 ? paidAmount : total);
  }

  return (
    <div className="flex flex-col gap-4 rounded border border-gray-200 bg-white p-4">
      {/* ── Totals ── */}
      <div className="space-y-1 text-sm">
        <div className="flex justify-between text-gray-600">
          <span>Sub-total</span>
          <span>₪{subTotal.toFixed(2)}</span>
        </div>
        {taxAmount > 0 && (
          <div className="flex justify-between text-gray-600">
            <span>Tax</span>
            <span>₪{taxAmount.toFixed(2)}</span>
          </div>
        )}
        <div className="flex justify-between border-t border-gray-200 pt-1 text-base
                        font-bold text-gray-900">
          <span>Total</span>
          <span>₪{total.toFixed(2)}</span>
        </div>
      </div>

      {/* ── Payment method ── */}
      <div>
        <label className="mb-1 block text-xs font-medium text-gray-600 uppercase tracking-wide">
          Payment Method
        </label>
        <div className="flex gap-2">
          {PAYMENT_METHODS.map((pm) => (
            <button
              key={pm.value}
              type="button"
              disabled={isPending}
              onClick={() => setMethod(pm.value)}
              className={`flex-1 rounded border py-1.5 text-sm font-medium transition-colors
                ${method === pm.value
                  ? 'border-blue-600 bg-blue-600 text-white'
                  : 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
                } disabled:opacity-40`}
            >
              {pm.label}
            </button>
          ))}
        </div>
      </div>

      {/* ── Cash tendered (Cash only) ── */}
      {method === 1 && (
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600 uppercase tracking-wide">
            Cash Tendered (₪)
          </label>
          <input
            type="number"
            min={0}
            step={0.01}
            value={paidInput}
            onChange={(e) => setPaidInput(e.target.value)}
            placeholder={total.toFixed(2)}
            disabled={isPending}
            className="w-full rounded border border-gray-300 px-3 py-2 text-right
                       text-sm [appearance:textfield] focus:outline-none
                       focus:ring-1 focus:ring-blue-400 disabled:opacity-40"
          />
          {shortfall && (
            <p className="mt-1 text-xs text-red-500">
              Short by ₪{(total - paidAmount).toFixed(2)}
            </p>
          )}
          {!shortfall && paidAmount > 0 && (
            <div className="mt-2 flex justify-between rounded bg-green-50 px-3 py-1.5
                            text-sm font-semibold text-green-700">
              <span>Change</span>
              <span>₪{changeAmount.toFixed(2)}</span>
            </div>
          )}
        </div>
      )}

      {/* ── Error ── */}
      {!!error && (
        <p className="rounded bg-red-50 px-3 py-2 text-sm text-red-700">
          {extractErrorMessage(error)}
        </p>
      )}

      {/* ── Confirm button ── */}
      <button
        type="button"
        disabled={!canConfirm}
        onClick={handleConfirm}
        className="w-full rounded bg-blue-600 py-3 text-base font-bold text-white
                   hover:bg-blue-700 disabled:opacity-40 transition-colors"
      >
        {isPending ? 'Processing…' : 'Confirm Sale'}
      </button>
    </div>
  );
}

