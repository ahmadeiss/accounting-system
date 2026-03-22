import type { CartLine, CartAction } from '@/types/sales';
import { CartItemRow } from './CartItemRow';

interface CartTableProps {
  lines:    CartLine[];
  dispatch: React.Dispatch<CartAction>;
  disabled: boolean;
}

/**
 * Scrollable cart table.
 * Empty state is a centered prompt — no spinner, no skeleton.
 */
export function CartTable({ lines, dispatch, disabled }: CartTableProps) {
  if (lines.length === 0) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2
                      rounded border-2 border-dashed border-gray-200 py-16
                      text-gray-400">
        <span className="text-4xl">🛒</span>
        <p className="text-sm">Scan a barcode or search for an item to start.</p>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto rounded border border-gray-200 bg-white">
      <table className="w-full text-sm">
        <thead className="sticky top-0 bg-gray-50 text-xs uppercase tracking-wide
                          text-gray-500 border-b border-gray-200">
          <tr>
            <th className="px-3 py-2 text-left">Item</th>
            <th className="px-2 py-2 text-left">Qty</th>
            <th className="px-2 py-2 text-right">Price (₪)</th>
            <th className="px-3 py-2 text-right">Total (₪)</th>
            <th className="px-2 py-2" />
          </tr>
        </thead>
        <tbody>
          {lines.map((line) => (
            <CartItemRow
              key={line.itemId}
              line={line}
              dispatch={dispatch}
              disabled={disabled}
            />
          ))}
        </tbody>
      </table>
    </div>
  );
}

