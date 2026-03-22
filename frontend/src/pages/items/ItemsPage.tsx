import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useItems, useToggleItemActive } from '@/features/items/hooks';
import { useAuthStore } from '@/features/auth/authStore';
import { useDebounce, formatCurrency } from '@/lib/utils';
import { Badge } from '@/components/ui/Badge';
import { PageSpinner } from '@/components/ui/Spinner';
import { ErrorMessage } from '@/components/ui/ErrorMessage';
import type { ItemsQuery } from '@/types/items';

const PAGE_SIZE = 20;

export default function ItemsPage() {
  const navigate = useNavigate();
  const hasPermission = useAuthStore((s) => s.hasPermission);
  const canWrite = hasPermission('items.write');

  const [search, setSearch]     = useState('');
  const [isActive, setIsActive] = useState<boolean | undefined>(undefined);
  const [page, setPage]         = useState(1);

  const debouncedSearch = useDebounce(search, 300);

  const query: ItemsQuery = {
    page,
    pageSize: PAGE_SIZE,
    search:   debouncedSearch || undefined,
    isActive,
  };

  const { data, isLoading, isError, error } = useItems(query);
  const toggleActive = useToggleItemActive();

  function handleSearchChange(e: React.ChangeEvent<HTMLInputElement>) {
    setSearch(e.target.value);
    setPage(1);
  }

  function handleStatusChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const v = e.target.value;
    setIsActive(v === '' ? undefined : v === 'true');
    setPage(1);
  }

  if (isLoading) return <PageSpinner />;
  if (isError)   return <ErrorMessage error={error} />;

  const { items = [], totalCount = 0, totalPages = 1 } = data ?? {};

  return (
    <div className="space-y-4">
      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-gray-900">Items</h1>
          <p className="text-sm text-gray-500">{totalCount} total</p>
        </div>
        {canWrite && (
          <div className="flex gap-2">
            <Link
              to="/imports/items"
              className="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              Import Excel
            </Link>
            <Link
              to="/items/new"
              className="rounded-md bg-primary-600 px-3 py-2 text-sm font-medium text-white hover:bg-primary-700"
            >
              + New Item
            </Link>
          </div>
        )}
      </div>

      {/* ── Filters ── */}
      <div className="flex gap-3">
        <input
          type="search"
          placeholder="Search name, SKU, barcode…"
          value={search}
          onChange={handleSearchChange}
          className="w-72 rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
        <select
          value={isActive === undefined ? '' : String(isActive)}
          onChange={handleStatusChange}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
        >
          <option value="">All statuses</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
      </div>

      {/* ── Table ── */}
      <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50">
            <tr>
              {['Name', 'SKU', 'Category', 'Unit', 'Cost', 'Sale Price', 'Tracking', 'Status', ''].map((h) => (
                <th key={h} className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wide text-gray-500">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {items.length === 0 ? (
              <tr>
                <td colSpan={9} className="py-12 text-center text-gray-400">
                  No items found.
                </td>
              </tr>
            ) : (
              items.map((item) => (
                <tr key={item.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900">{item.name}</td>
                  <td className="px-4 py-3 font-mono text-gray-600">{item.sku}</td>
                  <td className="px-4 py-3 text-gray-600">{item.categoryName}</td>
                  <td className="px-4 py-3 text-gray-600">{item.unitAbbreviation}</td>
                  <td className="px-4 py-3 text-gray-600">{formatCurrency(item.costPrice)}</td>
                  <td className="px-4 py-3 text-gray-600">{formatCurrency(item.salePrice)}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1">
                      {item.trackBatch  && <Badge variant="info">Batch</Badge>}
                      {item.trackExpiry && <Badge variant="warning">Expiry</Badge>}
                      {!item.trackBatch && !item.trackExpiry && <span className="text-gray-400">—</span>}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant={item.isActive ? 'success' : 'neutral'}>
                      {item.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      {canWrite && (
                        <button
                          onClick={() => navigate(`/items/${item.id}/edit`)}
                          className="text-primary-600 hover:underline"
                        >
                          Edit
                        </button>
                      )}
                      {canWrite && (
                        <button
                          onClick={() => toggleActive.mutate(item.id)}
                          disabled={toggleActive.isPending}
                          className="text-gray-500 hover:underline disabled:opacity-50"
                        >
                          {item.isActive ? 'Deactivate' : 'Activate'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* ── Pagination ── */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-gray-600">
          <span>Page {page} of {totalPages}</span>
          <div className="flex gap-2">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded border px-3 py-1 disabled:opacity-40"
            >
              Previous
            </button>
            <button
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="rounded border px-3 py-1 disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

