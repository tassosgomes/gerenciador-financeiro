import { waitFor } from '@testing-library/react';

import type { UserResponse } from '@/features/auth/types/auth';
import * as authApi from '@/features/auth/api/authApi';

import { AUTH_STORAGE_KEY, useAuthStore } from './authStore';

vi.mock('@/features/auth/api/authApi', () => ({
  loginApi: vi.fn(),
  logoutApi: vi.fn(),
  refreshTokenApi: vi.fn(),
}));

const mockUser: UserResponse = {
  id: '4abcbabe-e8da-41cf-bbb4-8f2c0058d8f2',
  name: 'Carlos Silva',
  email: 'carlos@gestorfinanceiro.com',
  role: 'Member',
  isActive: true,
  mustChangePassword: false,
  createdAt: '2026-01-01T00:00:00.000Z',
};

const memoryStorage = new Map<string, string>();

const localStorageMock = {
  getItem: (key: string) => memoryStorage.get(key) ?? null,
  setItem: (key: string, value: string) => {
    memoryStorage.set(key, value);
  },
  removeItem: (key: string) => {
    memoryStorage.delete(key);
  },
};

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
  configurable: true,
});

function clearPersistedAuthStateForTest(): void {
  window.localStorage.removeItem(AUTH_STORAGE_KEY);
}

function getPersistedAuthStateForTest(): string | null {
  const raw = window.localStorage.getItem(AUTH_STORAGE_KEY);
  return raw && raw.trim().length > 0 ? raw : null;
}

function resetAuthState(): void {
  useAuthStore.setState({
    accessToken: null,
    refreshToken: null,
    user: null,
    isAuthenticated: false,
    isLoading: false,
  });
  clearPersistedAuthStateForTest();
}

describe('authStore', () => {
  const loginApiMock = vi.mocked(authApi.loginApi);
  const logoutApiMock = vi.mocked(authApi.logoutApi);
  const refreshTokenApiMock = vi.mocked(authApi.refreshTokenApi);

  beforeEach(() => {
    resetAuthState();
    memoryStorage.clear();

    loginApiMock.mockResolvedValue({
      accessToken: 'access-token-1',
      refreshToken: 'refresh-token-1',
      expiresIn: 3600,
      user: mockUser,
    });

    refreshTokenApiMock.mockResolvedValue({
      accessToken: 'access-token-2',
      refreshToken: 'refresh-token-2',
      expiresIn: 3600,
      user: mockUser,
    });

    logoutApiMock.mockResolvedValue();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('logs in and persists tokens', async () => {
    await useAuthStore.getState().login('carlos@gestorfinanceiro.com', '123456');

    const state = useAuthStore.getState();

    expect(state.isAuthenticated).toBe(true);
    expect(state.accessToken).toBe('access-token-1');
    expect(state.refreshToken).toBe('refresh-token-1');
    expect(state.user?.name).toBe('Carlos Silva');
    expect(getPersistedAuthStateForTest()).toContain('refresh-token-1');
  });

  it('clears session on logout', async () => {
    await useAuthStore.getState().login('carlos@gestorfinanceiro.com', '123456');
    await useAuthStore.getState().logout();

    const state = useAuthStore.getState();

    expect(state.isAuthenticated).toBe(false);
    expect(state.accessToken).toBeNull();
    expect(state.refreshToken).toBeNull();
    expect(getPersistedAuthStateForTest()).toBeNull();
  });

  it('refreshes session and rotates tokens', async () => {
    await useAuthStore.getState().login('carlos@gestorfinanceiro.com', '123456');

    const refreshed = await useAuthStore.getState().refreshSession();

    expect(refreshed).toBe(true);
    expect(useAuthStore.getState().accessToken).toBe('access-token-2');
    expect(useAuthStore.getState().refreshToken).toBe('refresh-token-2');
  });

  it('hydrates persisted session and validates with refresh', async () => {
    window.localStorage.setItem(
      AUTH_STORAGE_KEY,
      JSON.stringify({
        refreshToken: 'refresh-token-1',
        user: mockUser,
      }),
    );

    useAuthStore.getState().hydrate();

    await waitFor(() => {
      expect(useAuthStore.getState().isLoading).toBe(false);
      expect(useAuthStore.getState().accessToken).toBe('access-token-2');
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
    });
  });

  it('clears session when refresh fails', async () => {
    refreshTokenApiMock.mockRejectedValueOnce(new Error('Unauthorized'));

    await useAuthStore.getState().login('carlos@gestorfinanceiro.com', '123456');
    const refreshed = await useAuthStore.getState().refreshSession();

    expect(refreshed).toBe(false);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(getPersistedAuthStateForTest()).toBeNull();
  });
});