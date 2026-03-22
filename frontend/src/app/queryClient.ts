import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime:          30_000,   // 30 s — dashboard data refreshes every 30 s
      gcTime:             300_000,  // 5 min cache
      retry:              1,
      refetchOnWindowFocus: true,
    },
    mutations: {
      retry: 0,
    },
  },
});

