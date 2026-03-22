import { useEffect } from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';
import { authService } from '@/features/auth/authService';

/**
 * Root layout for all authenticated pages.
 * - Listens for the 'auth:logout' event emitted by the Axios interceptor
 *   when a token refresh fails — forces redirect to /login.
 * - Sidebar + Topbar + scrollable main content area.
 */
export function AppShell() {
  const navigate = useNavigate();

  useEffect(() => {
    function onForceLogout() {
      authService.logout().then(() => {
        navigate('/login', { replace: true });
      });
    }

    window.addEventListener('auth:logout', onForceLogout);
    return () => window.removeEventListener('auth:logout', onForceLogout);
  }, [navigate]);

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50">
      {/* Sidebar — fixed width, full height */}
      <Sidebar />

      {/* Main column — topbar + scrollable content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        <Topbar />

        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

