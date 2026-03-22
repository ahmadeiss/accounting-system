import { useState } from 'react';
import type { CartLine, CartAction } from '@/types/sales';

interface CartItemRowProps {
  line:     CartLine;
  dispatch: React.Dispatch<CartAction>;
  /** Prevents edits while a sale is being confirmed. */
  disabled: boolean;
}

/**
 * Single cart row.
 *
 * Qty controls: − button | editable number input | + button
 * Price:        editable number input (override default sale price)
 * Remove:       × button on the right
 *
 * Inputs commit on blur/Enter. Invalid values are silently ignored.
 */
export function CartItemRow({ line, dispatch, disabled }: CartItemRowProps) {
  const [qtyInput, setQtyInput]     = useState(String(line.quantity));
  const [priceInput, setPriceInput] = useState(line.unitPrice.toFixed(2));

  // Sync controlled inputs when parent reducer updates (e.g., increment from scanner)
  const displayQty   = String(line.quantity);
  const displayPrice = line.unitPrice.toFixed(2);

  const lineTotal =
    line.quantity * line.unitPrice * (1 - line.discountPercent / 100);

  // ── Qty helpers ───────────────────────────────────────────────────────────

  function commitQty(raw: string) {
    const n = parseInt(raw, 10);
    if (!isNaN(n) && n >= 1) {
      dispatch({ type: 'SET_QTY', itemId: line.itemId, qty: n });
    } else {
      setQtyInput(displayQty); // revert
    }
  }

  // ── Price helpers ─────────────────────────────────────────────────────────

  function commitPrice(raw: string) {
    const n = parseFloat(raw);
    if (!isNaN(n) && n >= 0) {
      dispatch({ type: 'SET_PRICE', itemId: line.itemId, price: n });
    } else {
      setPriceInput(displayPrice); // revert
    }
  }

  return (
    <tr className="group border-b border-gray-100 hover:bg-gray-50">
      {/* Item name + SKU */}
      <td className="px-3 py-2">
        <div className="font-medium text-gray-800 text-sm leading-tight">
          {line.itemName}
        </div>
        <div className="font-mono text-xs text-gray-400">{line.itemSKU}</div>
      </td>

      {/* Quantity */}
      <td className="px-2 py-2 w-28">
        <div className="flex items-center gap-1">
          <button
            type="button"
            disabled={disabled}
            onClick={() => dispatch({ type: 'DEC_QTY', itemId: line.itemId })}
            className="flex h-6 w-6 items-center justify-center rounded border
                       border-gray-300 text-gray-600 text-xs hover:bg-gray-100
                       disabled:opacity-40"
          >
            −
          </button>

          <input
            type="number"
            min={1}
            disabled={disabled}
            value={qtyInput !== displayQty ? qtyInput : displayQty}
            onChange={(e) => setQtyInput(e.target.value)}
            onBlur={() => { commitQty(qtyInput); setQtyInput(displayQty); }}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                commitQty(qtyInput);
                (e.target as HTMLInputElement).blur();
              }
            }}
            className="w-12 rounded border border-gray-300 py-0.5 text-center
                       text-sm [appearance:textfield] focus:outline-none
                       focus:ring-1 focus:ring-blue-400 disabled:opacity-40"
          />

          <button
            type="button"
            disabled={disabled}
            onClick={() => dispatch({ type: 'INC_QTY', itemId: line.itemId })}
            className="flex h-6 w-6 items-center justify-center rounded border
                       border-gray-300 text-gray-600 text-xs hover:bg-gray-100
                       disabled:opacity-40"
          >
            +
          </button>
        </div>
      </td>

      {/* Unit price (editable) */}
      <td className="px-2 py-2 w-24">
        <input
          type="number"
          min={0}
          step={0.01}
          disabled={disabled}
          value={priceInput !== displayPrice ? priceInput : displayPrice}
          onChange={(e) => setPriceInput(e.target.value)}
          onBlur={() => { commitPrice(priceInput); setPriceInput(displayPrice); }}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              commitPrice(priceInput);
              (e.target as HTMLInputElement).blur();
            }
          }}
          className="w-full rounded border border-gray-300 py-0.5 px-1 text-right
                     text-sm [appearance:textfield] focus:outline-none
                     focus:ring-1 focus:ring-blue-400 disabled:opacity-40"
        />
      </td>

      {/* Line total */}
      <td className="px-3 py-2 w-24 text-right text-sm font-semibold text-gray-800">
        ₪{lineTotal.toFixed(2)}
      </td>

      {/* Remove */}
      <td className="px-2 py-2 w-8 text-right">
        <button
          type="button"
          disabled={disabled}
          onClick={() => dispatch({ type: 'REMOVE', itemId: line.itemId })}
          title="Remove line"
          className="text-gray-300 hover:text-red-500 disabled:opacity-30
                     transition-colors text-base leading-none"
        >
          ×
        </button>
      </td>
    </tr>
  );
}

