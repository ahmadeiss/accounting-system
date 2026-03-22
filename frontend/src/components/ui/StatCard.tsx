import { cn } from '@/lib/utils';
import type { ReactNode } from 'react';

interface StatCardProps {
  title:     string;
  value:     string | number;
  subtitle?: string;
  icon?:     ReactNode;
  variant?:  'default' | 'warning' | 'danger' | 'success';
  className?: string;
}

const variantStyles: Record<NonNullable<StatCardProps['variant']>, string> = {
  default: 'border-gray-200',
  warning: 'border-amber-300 bg-amber-50',
  danger:  'border-red-300  bg-red-50',
  success: 'border-green-300 bg-green-50',
};

const labelStyles: Record<NonNullable<StatCardProps['variant']>, string> = {
  default: 'text-gray-500',
  warning: 'text-amber-700',
  danger:  'text-red-700',
  success: 'text-green-700',
};

const valueStyles: Record<NonNullable<StatCardProps['variant']>, string> = {
  default: 'text-gray-900',
  warning: 'text-amber-900',
  danger:  'text-red-900',
  success: 'text-green-900',
};

export function StatCard({
  title,
  value,
  subtitle,
  icon,
  variant = 'default',
  className,
}: StatCardProps) {
  return (
    <div
      className={cn(
        'card p-5 flex items-start justify-between',
        variantStyles[variant],
        className
      )}
    >
      <div>
        <p className={cn('text-xs font-medium uppercase tracking-wide', labelStyles[variant])}>
          {title}
        </p>
        <p className={cn('mt-1 text-2xl font-bold tabular-nums', valueStyles[variant])}>
          {value}
        </p>
        {subtitle && (
          <p className="mt-1 text-xs text-gray-400">{subtitle}</p>
        )}
      </div>
      {icon && (
        <div className="text-2xl opacity-70 select-none">{icon}</div>
      )}
    </div>
  );
}

