import { useRef, useState, useEffect, type KeyboardEvent } from 'react';
import { useDebounce } from '@/lib/utils';
import { useItemSearch } from '../hooks';
import type { ItemDto } from '@/types/items';

interface BarcodeInputProps {
  /** Called when the cashier confirms an item (scan / Enter / click). */
  onSelect: (item: ItemDto) => void;
  /** Pass true after each successful add — triggers input clear + refocus. */
  lastAdded: number; // monotonic counter bumped by parent
}

/**
 * Unified barcode-scan / keyword-search input.
 *
 * Keyboard flow:
 *  - Any text → debounced search → dropdown appears
 *  - Enter     → select first result → clear + refocus
 *  - Escape    → clear input + close dropdown
 *  - ArrowDown → future: keyboard navigation in dropdown (deferred)
 *
 * Barcode scanner flow:
 *  - Scanner sends text + Enter character in <100 ms.
 *  - The Enter handler fires, finds the item by barcode, adds it.
 *  - Input clears and focus returns — cashier is ready for next scan.
 */
export function BarcodeInput({ onSelect, lastAdded }: BarcodeInputProps) {
  const inputRef            = useRef<HTMLInputElement>(null);
  const [value, setValue]   = useState('');
  const [open, setOpen]     = useState(false);

  const debouncedTerm = useDebounce(value, 250);
  const { data: results = [], isFetching } = useItemSearch(debouncedTerm);

  // ── Auto-focus on mount and after every successful add ────────────────────
  useEffect(() => {
    inputRef.current?.focus();
  }, [lastAdded]);

  // ── Show / hide dropdown based on results ─────────────────────────────────
  useEffect(() => {
    setOpen(results.length > 0 && value.trim().length > 0);
  }, [results, value]);

  function clearAndRefocus() {
    setValue('');
    setOpen(false);
    // Slight delay so the parent's state update fires before re-focus
    setTimeout(() => inputRef.current?.focus(), 50);
  }

  function handleSelect(item: ItemDto) {
    onSelect(item);
    clearAndRefocus();
  }

  function handleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') {
      e.preventDefault();
      if (results.length > 0) {
        handleSelect(results[0]);
      }
    }
    if (e.key === 'Escape') {
      clearAndRefocus();
    }
  }

  return (
    <div className="relative">
      {/* ── Search input ── */}
      <div className="relative">
        <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-gray-400">
          🔍
        </span>
        <input
          ref={inputRef}
          type="text"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          onBlur={() => setTimeout(() => setOpen(false), 150)}
          onFocus={() => results.length > 0 && value.trim() && setOpen(true)}
          placeholder="Scan barcode or search item…"
          autoComplete="off"
          spellCheck={false}
          className="w-full rounded border border-gray-300 bg-white py-2 pl-9 pr-3 text-sm
                     shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1
                     focus:ring-blue-500"
        />
        {isFetching && (
          <span className="absolute inset-y-0 right-3 flex items-center text-xs text-gray-400">
            …
          </span>
        )}
      </div>

      {/* ── Results dropdown ── */}
      {open && (
        <ul
          className="absolute z-50 mt-1 w-full rounded border border-gray-200 bg-white
                     shadow-lg"
        >
          {results.map((item) => (
            <li key={item.id}>
              <button
                type="button"
                onMouseDown={() => handleSelect(item)}   // mouseDown fires before blur
                className="flex w-full items-center gap-3 px-3 py-2 text-left text-sm
                           hover:bg-blue-50 focus:bg-blue-50"
              >
                <span className="flex-1 font-medium text-gray-800 truncate">
                  {item.name}
                </span>
                <span className="shrink-0 font-mono text-xs text-gray-400">
                  {item.sku}
                </span>
                <span className="shrink-0 text-xs font-semibold text-gray-700">
                  ₪{item.salePrice.toFixed(2)}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}

      <p className="mt-1 text-xs text-gray-400">
        Press <kbd className="rounded border px-1 py-0.5 text-gray-500">Enter</kbd> to add
        first match, or click a result.
      </p>
    </div>
  );
}

