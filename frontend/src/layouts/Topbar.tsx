import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/features/auth/authStore';
import { authService } from '@/features/auth/authService';

export function Topbar() {
  const user     = useAuthStore((s) => s.user);
  const navigate = useNavigate();

  async function handleLogout() {
    await authService.logout();
    navigate('/login', { replace: true });
  }

  return (
    <header className="flex h-16 items-center justify-between border-b border-gray-200 bg-white px-6">
      {/* Left — page context (breadcrumb can be added here later) */}
      <div />

      {/* Right — user info + logout */}
      <div className="flex items-center gap-4">
        {user && (
          <div className="flex items-center gap-3">
            {/* Avatar initials */}
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary-100 text-xs font-semibold text-primary-700 select-none">
              {user.fullName
                .split(' ')
                .slice(0, 2)
                .map((w) => w[0])
                .join('')
                .toUpperCase()}
            </div>
            <div className="hidden sm:block text-right">
              <p className="text-sm font-medium text-gray-900 leading-tight">
                {user.fullName}
              </p>
              <p className="text-xs text-gray-500 leading-tight">{user.roleName}</p>
            </div>
          </div>
        )}

        <button
          onClick={handleLogout}
          className="btn-ghost text-xs"
          title="Sign out"
        >
          Sign out
        </button>
      </div>
    </header>
  );
}

