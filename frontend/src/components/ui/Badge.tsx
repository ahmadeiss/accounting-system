import { cn } from '@/lib/utils';

type BadgeVariant = 'default' | 'info' | 'warning' | 'danger' | 'success' | 'neutral';

interface BadgeProps {
  children: React.ReactNode;
  variant?: BadgeVariant;
  className?: string;
}

const styles: Record<BadgeVariant, string> = {
  default: 'bg-gray-100  text-gray-700',
  info:    'bg-blue-100  text-blue-700',
  warning: 'bg-amber-100 text-amber-700',
  danger:  'bg-red-100   text-red-700',
  success: 'bg-green-100 text-green-700',
  neutral: 'bg-gray-100  text-gray-500',
};

export function Badge({ children, variant = 'default', className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        styles[variant],
        className
      )}
    >
      {children}
    </span>
  );
}

// ─── Convenience helpers for domain enums ────────────────────────────────────

export function severityBadge(severity: string) {
  const map: Record<string, BadgeVariant> = {
    Info:     'info',
    Warning:  'warning',
    Critical: 'danger',
  };
  return <Badge variant={map[severity] ?? 'default'}>{severity}</Badge>;
}

export function statusBadge(status: string) {
  const map: Record<string, BadgeVariant> = {
    Active:       'warning',
    Acknowledged: 'info',
    Resolved:     'success',
    Draft:        'neutral',
    Confirmed:    'success',
    Completed:    'success',
  };
  return <Badge variant={map[status] ?? 'default'}>{status}</Badge>;
}

