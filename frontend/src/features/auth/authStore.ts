import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { UserProfile } from '@/types/auth';

// ─── State shape ─────────────────────────────────────────────────────────────

interface AuthState {
  user:        UserProfile | null;
  isAuthenticated: boolean;
  // Actions
  setUser:  (user: UserProfile) => void;
  clearAuth: () => void;
  hasPermission: (permission: string) => boolean;
}

// ─── Store ────────────────────────────────────────────────────────────────────
// We persist only the user profile (NOT tokens — those live in tokenStorage).
// On mount, the app checks token validity via the bootstrap mechanism in App.tsx.

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user:            null,
      isAuthenticated: false,

      setUser: (user: UserProfile) =>
        set({ user, isAuthenticated: true }),

      clearAuth: () =>
        set({ user: null, isAuthenticated: false }),

      hasPermission: (permission: string) => {
        const { user } = get();
        return user?.permissions.includes(permission) ?? false;
      },
    }),
    {
      name: 'acc_auth',
      // Only persist user profile — tokens are in tokenStorage (localStorage directly)
      partialize: (state) => ({ user: state.user, isAuthenticated: state.isAuthenticated }),
    }
  )
);

