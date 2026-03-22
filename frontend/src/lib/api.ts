import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import type { LoginResponse } from '@/types/auth';

// ─── Token storage ────────────────────────────────────────────────────────────
// localStorage is acceptable for this single-tenant management system.
// Tokens are short-lived (60 min access / 7 day refresh).

const TOKEN_KEY   = 'acc_access_token';
const REFRESH_KEY = 'acc_refresh_token';

export const tokenStorage = {
  getAccess:      ()      => localStorage.getItem(TOKEN_KEY),
  getRefresh:     ()      => localStorage.getItem(REFRESH_KEY),
  set: (access: string, refresh: string) => {
    localStorage.setItem(TOKEN_KEY,   access);
    localStorage.setItem(REFRESH_KEY, refresh);
  },
  clear: () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
  },
};

// ─── Axios instance ───────────────────────────────────────────────────────────

export const api = axios.create({
  baseURL: '/api',        // Vite proxy forwards to backend in dev
  headers: { 'Content-Type': 'application/json' },
  timeout: 15_000,
});

// ─── Request interceptor: attach Bearer token ─────────────────────────────────

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = tokenStorage.getAccess();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ─── Refresh token lock ────────────────────────────────────────────────────────
// Prevents multiple concurrent 401 responses from each triggering a refresh.

let isRefreshing = false;
let refreshQueue: Array<{
  resolve: (token: string) => void;
  reject:  (err: unknown)  => void;
}> = [];

function processQueue(error: unknown, token: string | null) {
  refreshQueue.forEach(({ resolve, reject }) =>
    error ? reject(error) : resolve(token!)
  );
  refreshQueue = [];
}

// ─── Response interceptor: handle 401 → refresh → replay ─────────────────────

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Only attempt refresh on 401, once per request, and not on auth endpoints
    const isAuthEndpoint = original?.url?.startsWith('/auth');
    if (error.response?.status !== 401 || original._retry || isAuthEndpoint) {
      return Promise.reject(error);
    }

    const refreshToken = tokenStorage.getRefresh();
    if (!refreshToken) {
      // No refresh token — force logout via custom event
      window.dispatchEvent(new CustomEvent('auth:logout'));
      return Promise.reject(error);
    }

    if (isRefreshing) {
      // Queue this request until the ongoing refresh finishes
      return new Promise<string>((resolve, reject) => {
        refreshQueue.push({ resolve, reject });
      }).then((newToken) => {
        original.headers.Authorization = `Bearer ${newToken}`;
        return api(original);
      });
    }

    original._retry = true;
    isRefreshing     = true;

    try {
      const { data } = await axios.post<LoginResponse>(
        '/api/auth/refresh',
        { refreshToken },
        { headers: { 'Content-Type': 'application/json' } }
      );

      tokenStorage.set(data.accessToken, data.refreshToken);
      processQueue(null, data.accessToken);

      original.headers.Authorization = `Bearer ${data.accessToken}`;
      return api(original);
    } catch (refreshError) {
      processQueue(refreshError, null);
      tokenStorage.clear();
      window.dispatchEvent(new CustomEvent('auth:logout'));
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

