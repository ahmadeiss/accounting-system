import { NavLink } from 'react-router-dom';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/features/auth/authStore';

// ─── Nav item definition ──────────────────────────────────────────────────────

interface NavItem {
  label:      string;
  to:         string;
  icon:       string;   // Unicode emoji — avoids a heavy icon library dependency
  permission?: string;  // Required permission; omit = always visible when authenticated
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard',          to: '/dashboard',             icon: '▦',  permission: 'dashboard.read'  },
  { label: 'Alerts',             to: '/alerts',                icon: '⚠',  permission: 'alerts.read'     },
  { label: 'Items',              to: '/items',                 icon: '📦', permission: 'items.read'      },
  { label: 'Import Items',       to: '/imports/items',         icon: '⬆',  permission: 'imports.run'     },
  { label: 'Purchases',          to: '/purchases',             icon: '🛒', permission: 'purchases.read'  },
  { label: 'Opening Stock',      to: '/imports/opening-stock', icon: '📥', permission: 'imports.run'     },
  { label: 'POS Register',       to: '/pos',                   icon: '🖥', permission: 'sales.read'      },
];

// ─── Component ────────────────────────────────────────────────────────────────

export function Sidebar() {
  const hasPermission = useAuthStore((s) => s.hasPermission);

  const visibleItems = NAV_ITEMS.filter(
    (item) => !item.permission || hasPermission(item.permission)
  );

  return (
    <aside className="flex h-full w-60 flex-col border-r border-gray-200 bg-white">
      {/* Brand */}
      <div className="flex h-16 items-center border-b border-gray-200 px-6">
        <span className="text-lg font-bold text-primary-700 tracking-tight">
          Accounting
        </span>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4 px-3">
        <ul className="space-y-1">
          {visibleItems.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-primary-50 text-primary-700'
                      : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                  )
                }
              >
                <span className="text-base leading-none">{item.icon}</span>
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      {/* Version footer */}
      <div className="border-t border-gray-200 px-6 py-3">
        <p className="text-xs text-gray-400">v0.1.0</p>
      </div>
    </aside>
  );
}

