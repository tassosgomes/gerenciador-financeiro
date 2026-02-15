import { http, HttpResponse } from 'msw';
import { waitFor } from '@testing-library/react';

import { server } from '@/shared/test/mocks/server';

import { AUTH_STORAGE_KEY, useAuthStore } from './authStore';

function resetAuthState(): void {
  useAuthStore.setState({
    accessToken: null,
    refreshToken: null,
    user: null,
    isAuthenticated: false,
    isLoading: false,
  });
  window.localStorage.clear();
}

describe('authStore', () => {
  beforeEach(() => {
    resetAuthState();
  });

  it('logs in and persists tokens', async () => {
    await useAuthStore.getState().login('carlos@gestorfinanceiro.com', '123456');

    const state = useAuthStore.getState();

    expect(state.isAuthenticated).toBe(true);
    expect(state.accessToken).toBe('access-token-1');
    expect(state.refreshToken).toBe('refresh-token-1');
    expect(state.user?.name).toBe('Carlos Silva');
    expect(window.localStorage.getItem(AUTH_STORAGE_KEY)).toContain('refresh-token-1');
  });

  it('clears session on logout', async () => {
    await useAuthStore.getState().login('carlos@gestorfinanceiro.com', '123456');
    await useAuthStore.getState().logout();

    const state = useAuthStore.getState();

    expect(state.isAuthenticated).toBe(false);
    expect(state.accessToken).toBeNull();
    expect(state.refreshToken).toBeNull();
    expect(window.localStorage.getItem(AUTH_STORAGE_KEY)).toBeNull();
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
        accessToken: 'old-access-token',
        refreshToken: 'refresh-token-1',
        user: {
          id: '4abcbabe-e8da-41cf-bbb4-8f2c0058d8f2',
          name: 'Carlos Silva',
          email: 'carlos@gestorfinanceiro.com',
          role: 'Member',
          isActive: true,
          mustChangePassword: false,
          createdAt: '2026-01-01T00:00:00.000Z',
        },
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
    server.use(
      http.post('*/api/v1/auth/refresh', () => {
        return HttpResponse.json({ detail: 'Unauthorized' }, { status: 401 });
      }),
    );

    await useAuthStore.getState().login('carlos@gestorfinanceiro.com', '123456');
    const refreshed = await useAuthStore.getState().refreshSession();

    expect(refreshed).toBe(false);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(window.localStorage.getItem(AUTH_STORAGE_KEY)).toBeNull();
  });
});
