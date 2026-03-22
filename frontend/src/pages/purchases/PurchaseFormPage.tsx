import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm, useFieldArray, useWatch, type Control } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useCreatePurchase, useSuppliers, useWarehouses } from '@/features/purchases/hooks';
import { useItems } from '@/features/items/hooks';
import { PageSpinner } from '@/components/ui/Spinner';
import { extractErrorMessage, formatCurrency } from '@/lib/utils';
import type { ItemDto } from '@/types/items';

// ─── Schema ───────────────────────────────────────────────────────────────────

const lineSchema = z.object({
  itemId:          z.string().min(1, 'Select an item'),
  quantity:        z.coerce.number().positive('Must be > 0'),
  unitCost:        z.coerce.number().min(0, 'Must be ≥ 0'),
  discountPercent: z.coerce.number().min(0).max(100),
  taxPercent:      z.coerce.number().min(0).max(100),
  batchNumber:     z.string().optional(),
  productionDate:  z.string().optional(),
  expiryDate:      z.string().optional(),
  notes:           z.string().optional(),
  // UI-only: set when an item is chosen from the list
  _trackBatch:  z.boolean().optional(),
  _trackExpiry: z.boolean().optional(),
});

const schema = z.object({
  supplierId:  z.string().min(1, 'Supplier is required'),
  warehouseId: z.string().min(1, 'Warehouse is required'),
  // branchId derived from warehouse — stored as hidden form value
  branchId:    z.string().min(1, 'Warehouse has no branch info'),
  invoiceDate: z.string().min(1, 'Invoice date is required'),
  dueDate:     z.string().optional(),
  notes:       z.string().optional(),
  lines: z.array(lineSchema).min(1, 'Add at least one line'),
}).superRefine((data, ctx) => {
  data.lines.forEach((line, i) => {
    if (line._trackBatch && !line.batchNumber?.trim()) {
      ctx.addIssue({ code: 'custom', path: ['lines', i, 'batchNumber'],
        message: 'Batch number required for this item' });
    }
    if (line._trackExpiry && !line.expiryDate?.trim()) {
      ctx.addIssue({ code: 'custom', path: ['lines', i, 'expiryDate'],
        message: 'Expiry date required for this item' });
    }
  });
});

type FormValues = z.infer<typeof schema>;

const emptyLine = (): FormValues['lines'][0] => ({
  itemId: '', quantity: 1, unitCost: 0,
  discountPercent: 0, taxPercent: 0,
  batchNumber: '', productionDate: '', expiryDate: '', notes: '',
  _trackBatch: false, _trackExpiry: false,
});

// ─── Main page ────────────────────────────────────────────────────────────────

export default function PurchaseFormPage() {
  const navigate = useNavigate();
  const { data: suppliers,  isLoading: suppLoading  } = useSuppliers();
  const { data: warehouses, isLoading: whLoading    } = useWarehouses();
  const { data: itemsPage,  isLoading: itemsLoading } = useItems({ page: 1, pageSize: 500, isActive: true });
  const createPurchase = useCreatePurchase();
  const [mutError, setMutError] = useState<unknown>(null);

  const { register, control, handleSubmit, setValue, watch,
    formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      supplierId: '', warehouseId: '', branchId: '',
      invoiceDate: new Date().toISOString().split('T')[0],
      lines: [emptyLine()],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });
  const lines     = watch('lines');
  const grandTotal = lines.reduce((sum, l) => {
    const qty  = Number(l.quantity  ?? 0);
    const cost = Number(l.unitCost  ?? 0);
    const disc = Number(l.discountPercent ?? 0);
    const tax  = Number(l.taxPercent      ?? 0);
    return sum + qty * cost * (1 - disc / 100) * (1 + tax / 100);
  }, 0);

  const items: ItemDto[] = itemsPage?.items ?? [];

  if (suppLoading || whLoading || itemsLoading) return <PageSpinner />;

  async function onSubmit(values: FormValues) {
    setMutError(null);
    try {
      const { id } = await createPurchase.mutateAsync({
        supplierId:  values.supplierId,
        branchId:    values.branchId,
        warehouseId: values.warehouseId,
        invoiceDate: values.invoiceDate,
        dueDate:     values.dueDate || undefined,
        notes:       values.notes   || undefined,
        lines: values.lines.map((l) => ({
          itemId:          l.itemId,
          quantity:        Number(l.quantity),
          unitCost:        Number(l.unitCost),
          discountPercent: Number(l.discountPercent),
          taxPercent:      Number(l.taxPercent),
          batchNumber:     l.batchNumber     || undefined,
          productionDate:  l.productionDate  || undefined,
          expiryDate:      l.expiryDate      || undefined,
          notes:           l.notes           || undefined,
        })),
      });
      navigate(`/purchases/${id}`);
    } catch (err) {
      setMutError(err);
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 max-w-5xl">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-gray-900">New Purchase Invoice</h1>
      </div>

      {/* ── Header fields ── */}
      <div className="rounded border border-gray-200 bg-white p-4 shadow-sm space-y-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {/* Supplier */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">
              Supplier <span className="text-red-500">*</span>
            </label>
            <select {...register('supplierId')}
              className="rounded border border-gray-300 px-2 py-1.5 text-sm">
              <option value="">— select supplier —</option>
              {suppliers?.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            {errors.supplierId && (
              <p className="text-xs text-red-600">{errors.supplierId.message}</p>
            )}
          </div>

          {/* Warehouse (auto-sets branchId) */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">
              Warehouse <span className="text-red-500">*</span>
            </label>
            <select
              {...register('warehouseId')}
              onChange={(e) => {
                const wh = warehouses?.find((w) => w.id === e.target.value);
                setValue('warehouseId', e.target.value);
                setValue('branchId', wh?.branchId ?? '');
              }}
              className="rounded border border-gray-300 px-2 py-1.5 text-sm">
              <option value="">— select warehouse —</option>
              {warehouses?.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.branchName})
                </option>
              ))}
            </select>
            {errors.warehouseId && (
              <p className="text-xs text-red-600">{errors.warehouseId.message}</p>
            )}
          </div>

          {/* Invoice date */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">
              Invoice Date <span className="text-red-500">*</span>
            </label>
            <input type="date" {...register('invoiceDate')}
              className="rounded border border-gray-300 px-2 py-1.5 text-sm" />
            {errors.invoiceDate && (
              <p className="text-xs text-red-600">{errors.invoiceDate.message}</p>
            )}
          </div>

          {/* Due date (optional) */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Due Date</label>
            <input type="date" {...register('dueDate')}
              className="rounded border border-gray-300 px-2 py-1.5 text-sm" />
          </div>

          {/* Notes */}
          <div className="flex flex-col gap-1 sm:col-span-2">
            <label className="text-sm font-medium text-gray-700">Notes</label>
            <input {...register('notes')} placeholder="Optional notes"
              className="rounded border border-gray-300 px-2 py-1.5 text-sm" />
          </div>
        </div>
      </div>

      {/* ── Lines ── */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-medium text-gray-700">
            Invoice Lines {errors.lines?.root?.message && (
              <span className="text-red-600 text-xs ml-2">{errors.lines.root.message}</span>
            )}
          </h2>
          <button type="button"
            onClick={() => append(emptyLine())}
            className="text-sm text-blue-600 hover:underline"
          >+ Add Line</button>
        </div>

        {fields.map((field, i) => (
          <LineRow
            key={field.id}
            index={i}
            control={control}
            register={register}
            errors={errors}
            items={items}
            setValue={setValue}
            onRemove={() => remove(i)}
          />
        ))}
      </div>

      {/* ── Grand total + submit ── */}
      <div className="flex items-center justify-between rounded border border-gray-200 bg-white p-4 shadow-sm">
        <div className="text-sm text-gray-600">
          <span className="font-medium text-gray-800">Grand Total: </span>
          {formatCurrency(grandTotal)}
        </div>
        <div className="flex items-center gap-3">
          {!!mutError && (
            <span className="text-sm text-red-600">{extractErrorMessage(mutError)}</span>
          )}
          <button type="button" onClick={() => navigate('/purchases')}
            className="rounded border px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50">
            Cancel
          </button>
          <button type="submit" disabled={isSubmitting}
            className="rounded bg-blue-600 px-4 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50">
            {isSubmitting ? 'Saving…' : 'Save Draft'}
          </button>
        </div>
      </div>
    </form>
  );
}

// ─── Line row ─────────────────────────────────────────────────────────────────

interface LineRowProps {
  index:    number;
  control:  Control<FormValues>;
  register: ReturnType<typeof useForm<FormValues>>['register'];
  errors:   ReturnType<typeof useForm<FormValues>>['formState']['errors'];
  items:    ItemDto[];
  setValue: ReturnType<typeof useForm<FormValues>>['setValue'];
  onRemove: () => void;
}

function LineRow({ index, register, control, errors, items, setValue, onRemove }: LineRowProps) {
  const line    = useWatch({ control, name: `lines.${index}` });
  const lineErr = errors.lines?.[index];

  function onItemChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const id = e.target.value;
    setValue(`lines.${index}.itemId`, id);
    const item = items.find((it) => it.id === id);
    setValue(`lines.${index}._trackBatch`,  item?.trackBatch  ?? false);
    setValue(`lines.${index}._trackExpiry`, item?.trackExpiry ?? false);
    if (item?.costPrice) setValue(`lines.${index}.unitCost`, item.costPrice);
  }

  const lineTotal = (line.quantity ?? 0) * (line.unitCost ?? 0)
    * (1 - (line.discountPercent ?? 0) / 100)
    * (1 + (line.taxPercent      ?? 0) / 100);

  const field = (name: keyof FormValues['lines'][0]) =>
    `lines.${index}.${name}` as const;

  return (
    <div className="rounded border border-gray-200 bg-gray-50 p-3 space-y-2">
      {/* Row 1: item, qty, cost, discount, tax, total, remove */}
      <div className="flex flex-wrap gap-2 items-end">
        {/* Item selector */}
        <div className="flex flex-col gap-1 min-w-[180px] flex-1">
          <label className="text-xs text-gray-500">Item</label>
          <select
            value={line.itemId}
            onChange={onItemChange}
            className="rounded border border-gray-300 px-2 py-1.5 text-sm"
          >
            <option value="">— select item —</option>
            {items.map((it) => (
              <option key={it.id} value={it.id}>
                {it.name} ({it.sku})
              </option>
            ))}
          </select>
          {lineErr?.itemId && (
            <p className="text-xs text-red-600">{lineErr.itemId.message}</p>
          )}
        </div>

        {/* Qty */}
        <div className="flex flex-col gap-1 w-20">
          <label className="text-xs text-gray-500">Qty</label>
          <input type="number" step="0.001" min="0.001"
            {...register(field('quantity'))}
            className="rounded border border-gray-300 px-2 py-1.5 text-sm"
          />
          {lineErr?.quantity && (
            <p className="text-xs text-red-600">{lineErr.quantity.message}</p>
          )}
        </div>

        {/* Unit cost */}
        <div className="flex flex-col gap-1 w-24">
          <label className="text-xs text-gray-500">Unit Cost</label>
          <input type="number" step="0.01" min="0"
            {...register(field('unitCost'))}
            className="rounded border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>

        {/* Discount % */}
        <div className="flex flex-col gap-1 w-20">
          <label className="text-xs text-gray-500">Disc %</label>
          <input type="number" step="0.01" min="0" max="100"
            {...register(field('discountPercent'))}
            className="rounded border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>

        {/* Tax % */}
        <div className="flex flex-col gap-1 w-20">
          <label className="text-xs text-gray-500">Tax %</label>
          <input type="number" step="0.01" min="0" max="100"
            {...register(field('taxPercent'))}
            className="rounded border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>

        {/* Line total */}
        <div className="flex flex-col gap-1 w-24 text-right">
          <label className="text-xs text-gray-500">Total</label>
          <span className="py-1.5 text-sm font-medium text-gray-800">
            {formatCurrency(lineTotal)}
          </span>
        </div>

        <button type="button" onClick={onRemove}
          className="mb-0.5 text-red-500 hover:text-red-700 text-lg leading-none"
          title="Remove line"
        >×</button>
      </div>

      {/* Row 2: batch/expiry fields — shown only when item requires them */}
      {(line._trackBatch || line._trackExpiry) && (
        <div className="flex flex-wrap gap-2 pt-1 border-t border-gray-200">
          {line._trackBatch && (
            <div className="flex flex-col gap-1 flex-1 min-w-[140px]">
              <label className="text-xs text-gray-500">
                Batch Number <span className="text-red-500">*</span>
              </label>
              <input {...register(field('batchNumber'))}
                placeholder="e.g. LOT-2025-001"
                className="rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
              {lineErr?.batchNumber && (
                <p className="text-xs text-red-600">{lineErr.batchNumber.message}</p>
              )}
            </div>
          )}
          <div className="flex flex-col gap-1 w-36">
            <label className="text-xs text-gray-500">Production Date</label>
            <input type="date" {...register(field('productionDate'))}
              className="rounded border border-gray-300 px-2 py-1.5 text-sm"
            />
          </div>
          {line._trackExpiry && (
            <div className="flex flex-col gap-1 w-36">
              <label className="text-xs text-gray-500">
                Expiry Date <span className="text-red-500">*</span>
              </label>
              <input type="date" {...register(field('expiryDate'))}
                className="rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
              {lineErr?.expiryDate && (
                <p className="text-xs text-red-600">{lineErr.expiryDate.message}</p>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

