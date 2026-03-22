import { useRef, useState } from 'react';
import { useImportItems } from '@/features/imports/hooks';
import { Badge } from '@/components/ui/Badge';
import { Spinner } from '@/components/ui/Spinner';
import { extractErrorMessage } from '@/lib/utils';
import type { ImportResult, ImportRowResult } from '@/types/imports';

export default function ItemImportPage() {
  const fileRef  = useRef<HTMLInputElement>(null);
  const [file, setFile]       = useState<File | null>(null);
  const [dryRun, setDryRun]   = useState(true);
  const [result, setResult]   = useState<ImportResult | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);

  const importItems = useImportItems();

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0] ?? null;
    setFile(f);
    setResult(null);
    setApiError(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!file) return;
    setResult(null);
    setApiError(null);
    try {
      const res = await importItems.mutateAsync({ file, dryRun });
      setResult(res);
    } catch (err) {
      setApiError(extractErrorMessage(err));
    }
  }

  function handleReset() {
    setFile(null);
    setResult(null);
    setApiError(null);
    if (fileRef.current) fileRef.current.value = '';
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {/* ── Header ── */}
      <div>
        <h1 className="text-xl font-semibold text-gray-900">Import Items from Excel</h1>
        <p className="mt-1 text-sm text-gray-500">
          Upload an <code>.xlsx</code> file. Use <strong>Dry Run</strong> to validate without saving.
        </p>
      </div>

      {/* ── Template hint ── */}
      <div className="rounded-md border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-700">
        Required columns: <strong>Name, SKU</strong> — Optional: Barcode, Category, Unit, Cost Price, Sale Price,
        Reorder Level, Track Batch, Track Expiry, Min Expiry Days, Description.
      </div>

      {/* ── Upload form ── */}
      <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border border-gray-200 bg-white p-6">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Excel File (.xlsx)</label>
          <input
            ref={fileRef}
            type="file"
            accept=".xlsx"
            onChange={handleFileChange}
            className="block w-full text-sm text-gray-600 file:mr-4 file:rounded-md file:border-0 file:bg-primary-50 file:px-4 file:py-2 file:text-sm file:font-medium file:text-primary-700 hover:file:bg-primary-100"
          />
          {file && (
            <p className="mt-1 text-xs text-gray-500">{file.name} — {(file.size / 1024).toFixed(1)} KB</p>
          )}
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={dryRun}
            onChange={(e) => setDryRun(e.target.checked)}
            className="rounded"
          />
          <span>
            <strong>Dry Run</strong> — validate only, do not save
          </span>
        </label>

        {apiError && (
          <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-600">{apiError}</p>
        )}

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={!file || importItems.isPending}
            className="flex items-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-50"
          >
            {importItems.isPending && <Spinner size="sm" />}
            {dryRun ? 'Validate File' : 'Import Now'}
          </button>
          {result && (
            <button type="button" onClick={handleReset}
              className="rounded-md border border-gray-300 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
              Reset
            </button>
          )}
        </div>
      </form>

      {/* ── Result panel ── */}
      {result && <ImportResultPanel result={result} />}
    </div>
  );
}

// ─── Result panel ─────────────────────────────────────────────────────────────

function ImportResultPanel({ result }: { result: ImportResult }) {
  const statusVariant = result.failedRows === 0 ? 'success' : result.successRows > 0 ? 'warning' : 'danger';

  return (
    <div className="space-y-4">
      {/* Summary cards */}
      <div className="grid grid-cols-4 gap-3">
        <SummaryCard label="Total Rows"    value={result.totalRows}    />
        <SummaryCard label="Succeeded"     value={result.successRows}  color="text-green-600" />
        <SummaryCard label="Failed"        value={result.failedRows}   color="text-red-600" />
        <SummaryCard label="Skipped"       value={result.skippedRows}  color="text-gray-500" />
      </div>

      {/* Status banner */}
      <div className="flex items-center gap-3 rounded-md border border-gray-200 bg-white px-4 py-3">
        <Badge variant={statusVariant}>{result.status}</Badge>
        {result.isDryRun && <Badge variant="info">Dry Run — nothing was saved</Badge>}
        {result.errorSummary && <span className="text-sm text-red-600">{result.errorSummary}</span>}
      </div>

      {/* Row-level errors */}
      {result.rows.some((r) => r.status !== 'Success') && (
        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
          <div className="border-b border-gray-200 px-4 py-3">
            <h2 className="text-sm font-medium text-gray-900">Row Details</h2>
          </div>
          <table className="min-w-full divide-y divide-gray-100 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['Row', 'Status', 'Error', 'Raw Data'].map((h) => (
                  <th key={h} className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {result.rows
                .filter((r) => r.status !== 'Success')
                .map((row) => <RowResultRow key={row.rowNumber} row={row} />)}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function SummaryCard({ label, value, color = 'text-gray-900' }: { label: string; value: number; color?: string }) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white px-4 py-3 text-center">
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      <p className="text-xs text-gray-500">{label}</p>
    </div>
  );
}

function RowResultRow({ row }: { row: ImportRowResult }) {
  const variant = row.status === 'Failed' ? 'danger' : row.status === 'Skipped' ? 'neutral' : 'success';
  let parsed: Record<string, unknown> = {};
  try { parsed = JSON.parse(row.rawData); } catch { /* ignore */ }

  return (
    <tr className="align-top">
      <td className="px-4 py-2 font-mono text-gray-600">{row.rowNumber}</td>
      <td className="px-4 py-2"><Badge variant={variant}>{row.status}</Badge></td>
      <td className="px-4 py-2 text-red-600">{row.errorMessage ?? '—'}</td>
      <td className="px-4 py-2 text-gray-500 text-xs">
        {Object.entries(parsed).slice(0, 4).map(([k, v]) => (
          <span key={k} className="mr-2"><strong>{k}:</strong> {String(v)}</span>
        ))}
      </td>
    </tr>
  );
}

