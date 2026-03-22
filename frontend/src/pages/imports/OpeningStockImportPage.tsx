import { useRef, useState } from 'react';
import { useImportOpeningStock } from '@/features/imports/hooks';
import { useWarehouses } from '@/features/purchases/hooks';
import { Badge } from '@/components/ui/Badge';
import { Spinner } from '@/components/ui/Spinner';
import { extractErrorMessage } from '@/lib/utils';
import type { ImportResult, ImportRowResult } from '@/types/imports';

const TEMPLATE_COLUMNS = 'SKU | Quantity | CostPerUnit | BatchNumber | ExpiryDate (YYYY-MM-DD) | ProductionDate (YYYY-MM-DD)';

// ─── Summary card ─────────────────────────────────────────────────────────────

function SummaryCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className={`rounded border p-3 text-center ${color}`}>
      <div className="text-2xl font-bold">{value}</div>
      <div className="mt-0.5 text-xs font-medium uppercase tracking-wide">{label}</div>
    </div>
  );
}

// ─── Row errors table ─────────────────────────────────────────────────────────

function RowErrorsTable({ rows }: { rows: ImportRowResult[] }) {
  const failed = rows.filter((r) => r.status !== 'Success');
  if (failed.length === 0) return null;

  return (
    <div className="overflow-x-auto rounded border border-red-200 bg-white">
      <table className="min-w-full divide-y divide-gray-100 text-sm">
        <thead className="bg-red-50 text-xs font-medium uppercase text-red-700">
          <tr>
            {['Row', 'Status', 'Error', 'SKU', 'Qty', 'Batch', 'Expiry'].map((h) => (
              <th key={h} className="px-3 py-2 text-left">{h}</th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {failed.map((row) => {
            let raw: Record<string, string> = {};
            try { raw = JSON.parse(row.rawData ?? '{}'); } catch { /* noop */ }
            return (
              <tr key={row.rowNumber} className="hover:bg-gray-50">
                <td className="px-3 py-2 font-mono text-xs">{row.rowNumber}</td>
                <td className="px-3 py-2">
                  <Badge variant={row.status === 'Failed' ? 'danger' : 'warning'}>
                    {row.status}
                  </Badge>
                </td>
                <td className="px-3 py-2 text-red-700 max-w-xs">{row.errorMessage ?? '—'}</td>
                <td className="px-3 py-2 font-mono text-xs">{raw['SKU'] ?? '—'}</td>
                <td className="px-3 py-2">{raw['Quantity'] ?? '—'}</td>
                <td className="px-3 py-2 text-xs text-gray-500">{raw['BatchNumber'] ?? '—'}</td>
                <td className="px-3 py-2 text-xs text-gray-500">{raw['ExpiryDate'] ?? '—'}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function OpeningStockImportPage() {
  const fileRef       = useRef<HTMLInputElement>(null);
  const [file, setFile]           = useState<File | null>(null);
  const [warehouseId, setWarehouse] = useState('');
  const [dryRun, setDryRun]       = useState(true);
  const [result, setResult]       = useState<ImportResult | null>(null);
  const [apiError, setApiError]   = useState<string | null>(null);

  const importStock = useImportOpeningStock();
  const { data: warehouses, isLoading: whLoading } = useWarehouses();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!file) { setApiError('Select a file first.'); return; }
    if (!warehouseId) { setApiError('Select a warehouse first.'); return; }
    setApiError(null);
    setResult(null);
    try {
      const res = await importStock.mutateAsync({ file, warehouseId, dryRun });
      setResult(res);
    } catch (err) {
      setApiError(extractErrorMessage(err));
    }
  }

  const succeeded = result?.successRows ?? 0;
  const failed    = result?.failedRows  ?? 0;
  const skipped   = result?.skippedRows ?? 0;

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h1 className="text-xl font-semibold text-gray-900">Opening Stock Import</h1>
        <p className="mt-1 text-sm text-gray-500">
          Upload an Excel file to import opening inventory balances.
          Existing stock may be affected — use Dry Run first.
        </p>
      </div>

      {/* ── Template hint ── */}
      <div className="rounded border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800">
        <p className="font-medium mb-1">Required Excel columns (in order):</p>
        <code className="text-xs">{TEMPLATE_COLUMNS}</code>
        <p className="mt-1 text-xs text-blue-600">
          Dates must be in YYYY-MM-DD format. BatchNumber and dates are optional for non-tracked items.
        </p>
      </div>

      {/* ── Form ── */}
      <form onSubmit={handleSubmit} className="rounded border border-gray-200 bg-white p-4 shadow-sm space-y-4">
        {/* Warehouse selector */}
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">
            Target Warehouse <span className="text-red-500">*</span>
          </label>
          {whLoading ? (
            <p className="text-sm text-gray-400">Loading warehouses…</p>
          ) : (
            <select
              value={warehouseId}
              onChange={(e) => setWarehouse(e.target.value)}
              className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm sm:max-w-xs"
            >
              <option value="">— select warehouse —</option>
              {warehouses?.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.branchName})
                </option>
              ))}
            </select>
          )}
        </div>

        {/* File picker */}
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">
            Excel File (.xlsx) <span className="text-red-500">*</span>
          </label>
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => fileRef.current?.click()}
              className="rounded border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-50"
            >
              Choose File
            </button>
            <span className="text-sm text-gray-500">
              {file ? file.name : 'No file selected'}
            </span>
          </div>
          <input
            ref={fileRef}
            type="file"
            accept=".xlsx,.xls"
            className="hidden"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
        </div>

        {/* Dry run toggle */}
        <label className="flex items-center gap-2 cursor-pointer select-none">
          <input
            type="checkbox"
            checked={dryRun}
            onChange={(e) => setDryRun(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300 text-blue-600"
          />
          <span className="text-sm text-gray-700">
            Dry Run — validate without committing stock changes
          </span>
        </label>

        {apiError && (
          <p className="rounded bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
            {apiError}
          </p>
        )}

        <button
          type="submit"
          disabled={importStock.isPending}
          className="flex items-center gap-2 rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {importStock.isPending && <Spinner className="h-4 w-4" />}
          {importStock.isPending
            ? 'Importing…'
            : dryRun
            ? 'Validate (Dry Run)'
            : 'Import Opening Stock'}
        </button>
      </form>

      {/* ── Results ── */}
      {result && (
        <div className="space-y-4">
          {/* Status banner */}
          <div className={`rounded border px-4 py-2 text-sm font-medium ${
            result.isDryRun
              ? 'border-blue-200 bg-blue-50 text-blue-800'
              : !result.hasErrors
              ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
              : 'border-red-200 bg-red-50 text-red-800'
          }`}>
            {result.isDryRun
              ? `🔍 Dry Run complete — no stock was changed. ${failed > 0 ? `${failed} errors found.` : 'No errors found.'}`
              : !result.hasErrors
              ? '✅ Import committed successfully.'
              : `❌ Import completed with ${failed} row error(s).`
            }
          </div>

          {/* Summary cards */}
          <div className="grid grid-cols-4 gap-3">
            <SummaryCard label="Total"     value={result.totalRows}   color="border-gray-200 bg-gray-50 text-gray-700" />
            <SummaryCard label="Succeeded" value={succeeded}          color="border-emerald-200 bg-emerald-50 text-emerald-700" />
            <SummaryCard label="Failed"    value={failed}             color="border-red-200 bg-red-50 text-red-700" />
            <SummaryCard label="Skipped"   value={skipped}            color="border-amber-200 bg-amber-50 text-amber-700" />
          </div>

          {/* Row errors */}
          <RowErrorsTable rows={result.rows} />
        </div>
      )}
    </div>
  );
}

