import { api, tokenStorage } from '@/lib/api';
import { useAuthStore } from './authStore';
import type { LoginRequest, LoginResponse } from '@/types/auth';

// ─── Auth service ─────────────────────────────────────────────────────────────
// Thin wrapper over /api/auth/* endpoints.
// All token storage side-effects happen here; components call these functions.

export const authService = {
  /**
   * Exchange credentials for tokens. Stores tokens and hydrates the auth store.
   */
  async login(request: LoginRequest): Promise<void> {
    const { data } = await api.post<LoginResponse>('/auth/login', request);
    tokenStorage.set(data.accessToken, data.refreshToken);
    useAuthStore.getState().setUser(data.user);
  },

  /**
   * Revoke the current refresh token and clear local auth state.
   */
  async logout(): Promise<void> {
    const refreshToken = tokenStorage.getRefresh();
    if (refreshToken) {
      try {
        await api.post('/auth/logout', { refreshToken });
      } catch {
        // Best-effort — clear locally regardless of server response
      }
    }
    tokenStorage.clear();
    useAuthStore.getState().clearAuth();
  },

  /**
   * Bootstrap check on app load: if we have a stored user + valid access token,
   * the store is already hydrated by zustand/persist.
   * If the access token is expired, the Axios interceptor will refresh it on the
   * first API call. No explicit bootstrap call needed.
   */
  isLoggedIn(): boolean {
    return useAuthStore.getState().isAuthenticated && !!tokenStorage.getAccess();
  },
};

