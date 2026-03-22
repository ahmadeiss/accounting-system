import { useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  useItem,
  useCreateItem,
  useUpdateItem,
  useCategories,
  useUnits,
} from '@/features/items/hooks';
import { PageSpinner } from '@/components/ui/Spinner';
import { ErrorMessage } from '@/components/ui/ErrorMessage';
import { extractErrorMessage } from '@/lib/utils';

// ─── Zod schema ───────────────────────────────────────────────────────────────

const schema = z
  .object({
    name:                   z.string().min(1, 'Name is required').max(300),
    sku:                    z.string().min(1, 'SKU is required').max(100),
    barcode:                z.string().max(100).optional().or(z.literal('')),
    description:            z.string().optional(),
    categoryId:             z.string().uuid('Select a category'),
    unitId:                 z.string().uuid('Select a unit'),
    costPrice:              z.coerce.number().min(0, 'Must be ≥ 0'),
    salePrice:              z.coerce.number().min(0, 'Must be ≥ 0'),
    reorderLevel:           z.coerce.number().min(0, 'Must be ≥ 0'),
    trackBatch:             z.boolean(),
    trackExpiry:            z.boolean(),
    minExpiryDaysBeforeSale: z.coerce.number().int().min(0),
    isActive:               z.boolean(),
  })
  .refine((d) => !d.trackExpiry || d.trackBatch, {
    message: 'Track Batch must be enabled when Track Expiry is enabled',
    path: ['trackBatch'],
  });

type FormValues = z.infer<typeof schema>;

// ─── Component ────────────────────────────────────────────────────────────────

export default function ItemFormPage() {
  const { id } = useParams<{ id?: string }>();
  const isEdit  = !!id;
  const navigate = useNavigate();

  const { data: item, isLoading: itemLoading, isError: itemError } = useItem(id ?? '');
  const { data: categories = [], isLoading: catsLoading } = useCategories();
  const { data: units = [],      isLoading: unitsLoading } = useUnits();

  const createItem = useCreateItem();
  const updateItem = useUpdateItem(id ?? '');

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '', sku: '', barcode: '', description: '',
      categoryId: '', unitId: '',
      costPrice: 0, salePrice: 0, reorderLevel: 0,
      trackBatch: false, trackExpiry: false,
      minExpiryDaysBeforeSale: 0, isActive: true,
    },
  });

  // Populate form when editing
  useEffect(() => {
    if (item) {
      reset({
        name: item.name, sku: item.sku,
        barcode: item.barcode ?? '', description: item.description ?? '',
        categoryId: item.categoryId, unitId: item.unitId,
        costPrice: item.costPrice, salePrice: item.salePrice,
        reorderLevel: item.reorderLevel,
        trackBatch: item.trackBatch, trackExpiry: item.trackExpiry,
        minExpiryDaysBeforeSale: item.minExpiryDaysBeforeSale,
        isActive: item.isActive,
      });
    }
  }, [item, reset]);

  // TrackExpiry forces TrackBatch
  const trackExpiry = watch('trackExpiry');
  useEffect(() => {
    if (trackExpiry) setValue('trackBatch', true);
  }, [trackExpiry, setValue]);

  async function onSubmit(values: FormValues) {
    try {
      const body = {
        ...values,
        barcode:     values.barcode || undefined,
        description: values.description || undefined,
        trackBatch:  values.trackBatch || values.trackExpiry,
      };
      if (isEdit) {
        await updateItem.mutateAsync(body);
      } else {
        await createItem.mutateAsync(body);
      }
      navigate('/items');
    } catch (err) {
      // error displayed inline via mutation state
      console.error(err);
    }
  }

  if (isEdit && itemLoading) return <PageSpinner />;
  if (isEdit && itemError)   return <ErrorMessage error={new Error('Item not found.')} />;

  const lookupsLoading = catsLoading || unitsLoading;
  const mutationError  = isEdit ? updateItem.error : createItem.error;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-xl font-semibold text-gray-900">
        {isEdit ? 'Edit Item' : 'New Item'}
      </h1>

      {mutationError && (
        <ErrorMessage error={mutationError} title={extractErrorMessage(mutationError)} />
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5 rounded-lg border border-gray-200 bg-white p-6">
        {/* ── Basic info ── */}
        <div className="grid grid-cols-2 gap-4">
          <Field label="Name *" error={errors.name?.message}>
            <input {...register('name')} className={inputCls(!!errors.name)} />
          </Field>
          <Field label="SKU *" error={errors.sku?.message}>
            <input {...register('sku')} disabled={isEdit} className={inputCls(!!errors.sku)} />
          </Field>
          <Field label="Barcode" error={errors.barcode?.message}>
            <input {...register('barcode')} className={inputCls(!!errors.barcode)} />
          </Field>
          <Field label="Category *" error={errors.categoryId?.message}>
            <select {...register('categoryId')} disabled={lookupsLoading} className={inputCls(!!errors.categoryId)}>
              <option value="">Select…</option>
              {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </Field>
          <Field label="Unit *" error={errors.unitId?.message}>
            <select {...register('unitId')} disabled={lookupsLoading} className={inputCls(!!errors.unitId)}>
              <option value="">Select…</option>
              {units.map((u) => <option key={u.id} value={u.id}>{u.name} ({u.abbreviation})</option>)}
            </select>
          </Field>
        </div>

        <Field label="Description" error={errors.description?.message}>
          <textarea {...register('description')} rows={2} className={inputCls(false)} />
        </Field>

        {/* ── Pricing ── */}
        <div className="grid grid-cols-3 gap-4">
          <Field label="Cost Price *" error={errors.costPrice?.message}>
            <input type="number" step="0.0001" {...register('costPrice')} className={inputCls(!!errors.costPrice)} />
          </Field>
          <Field label="Sale Price *" error={errors.salePrice?.message}>
            <input type="number" step="0.0001" {...register('salePrice')} className={inputCls(!!errors.salePrice)} />
          </Field>
          <Field label="Reorder Level" error={errors.reorderLevel?.message}>
            <input type="number" step="0.0001" {...register('reorderLevel')} className={inputCls(!!errors.reorderLevel)} />
          </Field>
        </div>

        {/* ── Tracking ── */}
        <div className="space-y-2">
          <p className="text-sm font-medium text-gray-700">Tracking</p>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register('trackBatch')} disabled={trackExpiry} />
            Track Batch Number
            {errors.trackBatch && <span className="text-red-500">{errors.trackBatch.message}</span>}
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register('trackExpiry')} />
            Track Expiry Date <span className="text-gray-400">(forces batch tracking)</span>
          </label>
          {trackExpiry && (
            <Field label="Min Days Before Expiry for Sale" error={errors.minExpiryDaysBeforeSale?.message}>
              <input type="number" {...register('minExpiryDaysBeforeSale')} className={inputCls(!!errors.minExpiryDaysBeforeSale)} />
            </Field>
          )}
        </div>

        {/* ── Status (edit only) ── */}
        {isEdit && (
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register('isActive')} />
            Active
          </label>
        )}

        {/* ── Actions ── */}
        <div className="flex justify-end gap-3 pt-2">
          <button type="button" onClick={() => navigate('/items')}
            className="rounded-md border border-gray-300 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
            Cancel
          </button>
          <button type="submit" disabled={isSubmitting}
            className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-50">
            {isSubmitting ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Item'}
          </button>
        </div>
      </form>
    </div>
  );
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function inputCls(hasError: boolean) {
  return `w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 ${
    hasError ? 'border-red-400' : 'border-gray-300'
  }`;
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-gray-700">{label}</label>
      {children}
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  );
}

