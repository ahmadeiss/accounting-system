import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { authService } from '@/features/auth/authService';
import { useAuthStore } from '@/features/auth/authStore';

interface ProtectedRouteProps {
  /** If provided, user must have this permission — otherwise redirected to /. */
  permission?: string;
}

/**
 * Guards any route that requires authentication.
 * - Unauthenticated users are redirected to /login (with `from` preserved).
 * - Authenticated users missing a specific permission see a 403 page.
 */
export function ProtectedRoute({ permission }: ProtectedRouteProps) {
  const location     = useLocation();
  const hasPermission = useAuthStore((s) => s.hasPermission);

  if (!authService.isLoggedIn()) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (permission && !hasPermission(permission)) {
    return (
      <div className="flex h-screen items-center justify-center text-gray-500">
        <div className="text-center">
          <p className="text-4xl font-bold">403</p>
          <p className="mt-2 text-lg">You don&apos;t have access to this page.</p>
        </div>
      </div>
    );
  }

  return <Outlet />;
}

