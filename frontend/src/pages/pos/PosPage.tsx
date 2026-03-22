import { useState } from 'react';
import { useWarehouses } from '@/features/purchases/hooks';
import { useCart, useConfirmSale } from '@/features/pos/hooks';
import { BarcodeInput } from '@/features/pos/components/BarcodeInput';
import { CartTable } from '@/features/pos/components/CartTable';
import { PaymentPanel } from '@/features/pos/components/PaymentPanel';
import type { PosSession, PaymentMethod } from '@/types/sales';
import type { ItemDto } from '@/types/items';

// ─── Session setup screen ─────────────────────────────────────────────────────

function SessionSetup({ onStart }: { onStart: (s: PosSession) => void }) {
  const { data: warehouses = [], isLoading } = useWarehouses();
  const [warehouseId, setWarehouseId] = useState('');

  const selected = warehouses.find((w) => w.id === warehouseId);

  function handleStart() {
    if (!selected) return;
    onStart({
      warehouseId:   selected.id,
      branchId:      selected.branchId,
      warehouseName: selected.name,
    });
  }

  return (
    <div className="flex h-full items-center justify-center">
      <div className="w-full max-w-sm rounded border border-gray-200 bg-white p-8 shadow-sm">
        <h1 className="mb-6 text-xl font-bold text-gray-900">Start POS Session</h1>

        <label className="mb-1 block text-sm font-medium text-gray-700">
          Warehouse
        </label>
        <select
          value={warehouseId}
          onChange={(e) => setWarehouseId(e.target.value)}
          disabled={isLoading}
          className="mb-6 w-full rounded border border-gray-300 px-3 py-2 text-sm
                     focus:outline-none focus:ring-1 focus:ring-blue-500"
        >
          <option value="">— Select warehouse —</option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name} ({w.branchName})
            </option>
          ))}
        </select>

        <button
          type="button"
          disabled={!warehouseId}
          onClick={handleStart}
          className="w-full rounded bg-blue-600 py-2.5 text-sm font-semibold text-white
                     hover:bg-blue-700 disabled:opacity-40"
        >
          Open Register
        </button>
      </div>
    </div>
  );
}

// ─── Sale success banner ──────────────────────────────────────────────────────

function SuccessBanner({ invoiceId, onNew }: { invoiceId: string; onNew: () => void }) {
  return (
    <div className="flex h-full flex-col items-center justify-center gap-4">
      <div className="rounded-full bg-green-100 p-6 text-5xl">✓</div>
      <h2 className="text-2xl font-bold text-green-700">Sale Confirmed</h2>
      <p className="text-sm text-gray-500">Invoice #{invoiceId.slice(0, 8).toUpperCase()}</p>
      <button
        type="button"
        onClick={onNew}
        className="mt-4 rounded bg-blue-600 px-6 py-2.5 text-sm font-semibold
                   text-white hover:bg-blue-700"
      >
        New Sale
      </button>
    </div>
  );
}

// ─── Main POS page ────────────────────────────────────────────────────────────

export default function PosPage() {
  const [session, setSession]         = useState<PosSession | null>(null);
  const [lastAdded, setLastAdded]     = useState(0);
  const [confirmedId, setConfirmedId] = useState<string | null>(null);

  const { lines, dispatch, subTotal, taxAmount, total } = useCart();
  const confirmSale = useConfirmSale();

  // ── No session yet ────────────────────────────────────────────────────────
  if (!session) {
    return <SessionSetup onStart={setSession} />;
  }

  // ── Sale just confirmed ───────────────────────────────────────────────────
  if (confirmedId) {
    return (
      <SuccessBanner
        invoiceId={confirmedId}
        onNew={() => {
          setConfirmedId(null);
          dispatch({ type: 'CLEAR' });
          confirmSale.reset();
        }}
      />
    );
  }

  // ── Active session ────────────────────────────────────────────────────────
  function handleItemSelect(item: ItemDto) {
    dispatch({
      type: 'ADD_OR_INCREMENT',
      item: { id: item.id, name: item.name, sku: item.sku, salePrice: item.salePrice },
    });
    setLastAdded((n) => n + 1);
  }

  function handleConfirm(paymentMethod: PaymentMethod, paidAmount: number) {
    confirmSale.mutate(
      {
        branchId:      session!.branchId,
        warehouseId:   session!.warehouseId,
        customerId:    null,
        paymentMethod,
        paidAmount,
        notes:         null,
        lines: lines.map((l) => ({
          itemId:          l.itemId,
          quantity:        l.quantity,
          unitPrice:       l.unitPrice,
          discountPercent: l.discountPercent,
          taxPercent:      l.taxPercent,
        })),
      },
      { onSuccess: (id) => setConfirmedId(id) }
    );
  }

  return (
    <div className="flex h-full flex-col gap-4">
      {/* ── Header bar ── */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-bold text-gray-900">Point of Sale</h1>
          <p className="text-xs text-gray-500">{session.warehouseName}</p>
        </div>
        <button
          type="button"
          onClick={() => setSession(null)}
          className="text-xs text-gray-400 hover:text-gray-600"
        >
          Change Warehouse
        </button>
      </div>

      {/* ── Barcode / search input ── */}
      <BarcodeInput onSelect={handleItemSelect} lastAdded={lastAdded} />

      {/* ── Cart + payment (two-column) ── */}
      <div className="flex flex-1 gap-4 overflow-hidden">
        {/* Cart — takes remaining width */}
        <div className="flex flex-1 flex-col overflow-hidden">
          <CartTable
            lines={lines}
            dispatch={dispatch}
            disabled={confirmSale.isPending}
          />
        </div>

        {/* Payment panel — fixed width */}
        <div className="w-72 shrink-0">
          <PaymentPanel
            subTotal={subTotal}
            taxAmount={taxAmount}
            total={total}
            isEmpty={lines.length === 0}
            isPending={confirmSale.isPending}
            error={confirmSale.error}
            onConfirm={handleConfirm}
          />
        </div>
      </div>
    </div>
  );
}

