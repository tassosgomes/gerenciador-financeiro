import { http, HttpResponse } from 'msw';

import { server } from '@/shared/test/mocks/server';

import { apiClient, clearAuthSessionManager, registerAuthSessionManager } from './apiClient';

describe('apiClient interceptors', () => {
  afterEach(() => {
    clearAuthSessionManager();
  });

  it('injects bearer token on requests', async () => {
    registerAuthSessionManager({
      getAccessToken: () => 'token-123',
      refreshSession: async () => false,
      logout: async () => Promise.resolve(),
    });

    server.use(
      http.get('*/api/v1/interceptor-auth', ({ request }) => {
        return HttpResponse.json({ authorization: request.headers.get('authorization') });
      }),
    );

    const response = await apiClient.get<{ authorization: string }>('/api/v1/interceptor-auth');

    expect(response.data.authorization).toBe('Bearer token-123');
  });

  it('refreshes token and retries request after 401', async () => {
    let token = 'token-old';
    const refreshSession = vi.fn(async () => {
      token = 'token-new';
      return true;
    });
    const logout = vi.fn(async () => Promise.resolve());

    registerAuthSessionManager({
      getAccessToken: () => token,
      refreshSession,
      logout,
    });

    let requestCount = 0;

    server.use(
      http.get('*/api/v1/interceptor-protected', ({ request }) => {
        requestCount += 1;

        if (requestCount === 1) {
          return HttpResponse.json({ detail: 'Unauthorized' }, { status: 401 });
        }

        return HttpResponse.json({ authorization: request.headers.get('authorization') });
      }),
    );

    const response = await apiClient.get<{ authorization: string }>('/api/v1/interceptor-protected');

    expect(refreshSession).toHaveBeenCalledTimes(1);
    expect(logout).not.toHaveBeenCalled();
    expect(response.data.authorization).toBe('Bearer token-new');
  });

  it('logs out when refresh fails after 401', async () => {
    const logout = vi.fn(async () => Promise.resolve());
    const onSessionExpired = vi.fn();

    registerAuthSessionManager({
      getAccessToken: () => 'token-old',
      refreshSession: async () => false,
      logout,
      onSessionExpired,
    });

    server.use(
      http.get('*/api/v1/interceptor-failed-refresh', () => {
        return HttpResponse.json({ detail: 'Unauthorized' }, { status: 401 });
      }),
    );

    await expect(apiClient.get('/api/v1/interceptor-failed-refresh')).rejects.toBeTruthy();
    expect(logout).toHaveBeenCalledTimes(1);
    expect(onSessionExpired).toHaveBeenCalledTimes(1);
  });
});
