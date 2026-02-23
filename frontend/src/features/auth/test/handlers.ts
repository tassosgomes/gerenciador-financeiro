import { http, HttpResponse } from 'msw';

import type { AuthResponse, LoginRequest } from '@/features/auth/types/auth';

const BASE_URL = '*';

const defaultUser = {
  id: '4abcbabe-e8da-41cf-bbb4-8f2c0058d8f2',
  name: 'Carlos Silva',
  email: 'carlos@gestorfinanceiro.com',
  role: 'Member',
  isActive: true,
  mustChangePassword: false,
  createdAt: '2026-01-01T00:00:00.000Z',
};

export const authHandlers = [
  http.post(`${BASE_URL}/api/v1/auth/login`, async ({ request }) => {
    const payload = (await request.json()) as LoginRequest;

    if (payload.email === 'carlos@gestorfinanceiro.com' && payload.password === '123456') {
      const successResponse: AuthResponse = {
        accessToken: 'access-token-1',
        refreshToken: 'refresh-token-1',
        expiresIn: 3600,
        user: defaultUser,
      };

      return HttpResponse.json(successResponse);
    }

    return HttpResponse.json(
      {
        type: 'https://gestorfinanceiro.com/problems/InvalidCredentials',
        title: 'Unauthorized',
        status: 401,
        detail: 'Credenciais inválidas.',
      },
      { status: 401 },
    );
  }),

  http.post(`${BASE_URL}/api/v1/auth/refresh`, async ({ request }) => {
    const payload = (await request.json()) as { refreshToken?: string };

    if (payload.refreshToken === 'refresh-token-1' || payload.refreshToken === 'refresh-token-2') {
      const refreshResponse: AuthResponse = {
        accessToken: 'access-token-2',
        refreshToken: 'refresh-token-2',
        expiresIn: 3600,
        user: defaultUser,
      };

      return HttpResponse.json(refreshResponse);
    }

    return HttpResponse.json(
      {
        type: 'https://gestorfinanceiro.com/problems/InvalidRefreshToken',
        title: 'Unauthorized',
        status: 401,
        detail: 'Refresh token inválido.',
      },
      { status: 401 },
    );
  }),

  http.post(`${BASE_URL}/api/v1/auth/logout`, () => new HttpResponse(null, { status: 204 })),
];
