import { create } from 'zustand';

import { loginApi, logoutApi, refreshTokenApi } from '@/features/auth/api/authApi';
import type { UserResponse } from '@/features/auth/types/auth';
import { registerAuthSessionManager } from '@/shared/services/apiClient';

export const AUTH_STORAGE_KEY = 'gf.auth';

interface PersistedAuthState {
  accessToken: string;
  refreshToken: string;
  user: UserResponse;
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserResponse | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<boolean>;
  setTokens: (accessToken: string, refreshToken: string, user: UserResponse) => void;
  hydrate: () => void;
}

function readPersistedAuthState(): PersistedAuthState | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const rawState = window.localStorage.getItem(AUTH_STORAGE_KEY);

  if (!rawState) {
    return null;
  }

  try {
    const parsedState = JSON.parse(rawState) as PersistedAuthState;

    if (!parsedState.accessToken || !parsedState.refreshToken || !parsedState.user) {
      return null;
    }

    return parsedState;
  } catch {
    return null;
  }
}

function persistAuthState(state: PersistedAuthState): void {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(state));
}

function clearPersistedAuthState(): void {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.removeItem(AUTH_STORAGE_KEY);
}

function clearInMemoryAuthState(set: (partial: Partial<AuthState>) => void): void {
  clearPersistedAuthState();
  set({
    accessToken: null,
    refreshToken: null,
    user: null,
    isAuthenticated: false,
  });
}

export const useAuthStore = create<AuthState>((set, get) => ({
  accessToken: null,
  refreshToken: null,
  user: null,
  isAuthenticated: false,
  isLoading: false,

  setTokens: (accessToken, refreshToken, user) => {
    persistAuthState({ accessToken, refreshToken, user });
    set({
      accessToken,
      refreshToken,
      user,
      isAuthenticated: true,
    });
  },

  login: async (email, password) => {
    set({ isLoading: true });

    try {
      const response = await loginApi({ email, password });
      get().setTokens(response.accessToken, response.refreshToken, response.user);
    } finally {
      set({ isLoading: false });
    }
  },

  logout: async () => {
    set({ isLoading: true });

    try {
      if (get().isAuthenticated) {
        await logoutApi();
      }
    } catch {
      // noop
    } finally {
      clearInMemoryAuthState(set);
      set({ isLoading: false });
    }
  },

  refreshSession: async () => {
    const currentRefreshToken = get().refreshToken;

    if (!currentRefreshToken) {
      clearInMemoryAuthState(set);
      return false;
    }

    try {
      const response = await refreshTokenApi(currentRefreshToken);
      get().setTokens(response.accessToken, response.refreshToken, response.user);
      return true;
    } catch {
      clearInMemoryAuthState(set);
      return false;
    }
  },

  hydrate: () => {
    const persistedAuthState = readPersistedAuthState();

    if (!persistedAuthState) {
      set({ isLoading: false });
      return;
    }

    set({
      accessToken: persistedAuthState.accessToken,
      refreshToken: persistedAuthState.refreshToken,
      user: persistedAuthState.user,
      isAuthenticated: true,
      isLoading: true,
    });

    void get()
      .refreshSession()
      .finally(() => {
        set({ isLoading: false });
      });
  },
}));

registerAuthSessionManager({
  getAccessToken: () => useAuthStore.getState().accessToken,
  refreshSession: () => useAuthStore.getState().refreshSession(),
  logout: () => useAuthStore.getState().logout(),
  onSessionExpired: () => {
    if (typeof window !== 'undefined') {
      window.location.assign('/login?session=expired');
    }
  },
});
