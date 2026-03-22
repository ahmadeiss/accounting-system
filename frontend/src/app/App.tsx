import { RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from './queryClient';
import { router } from '@/routes';

/**
 * Root application component.
 *
 * Providers (outermost first):
 * 1. QueryClientProvider — TanStack Query for all server state
 * 2. RouterProvider      — React Router v6 data router
 *
 * Auth bootstrap: zustand/persist rehydrates auth state from localStorage on
 * mount. The first API call with a stale access token triggers a transparent
 * refresh via the Axios interceptor in lib/api.ts.
 */
export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  );
}

