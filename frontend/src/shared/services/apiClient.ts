import axios from 'axios';
import { AxiosHeaders } from 'axios';
import type { AxiosError, InternalAxiosRequestConfig } from 'axios';

import { API_URL } from '@/shared/config/runtimeConfig';

interface AuthSessionManager {
  getAccessToken: () => string | null;
  refreshSession: () => Promise<boolean>;
  logout: () => Promise<void>;
  onSessionExpired?: () => void;
}

type RetriableRequestConfig = InternalAxiosRequestConfig & {
  _retry?: boolean;
};

let authSessionManager: AuthSessionManager | null = null;
let refreshPromise: Promise<boolean> | null = null;

export const apiClient = axios.create({
  baseURL: API_URL,
  timeout: 30_000,
  headers: {
    'Content-Type': 'application/json',
  },
});

function setAuthorizationHeader(config: RetriableRequestConfig, token: string): void {
  const headers = AxiosHeaders.from(config.headers);
  headers.set('Authorization', `Bearer ${token}`);
  config.headers = headers;
}

function shouldTryRefresh(config?: RetriableRequestConfig): boolean {
  if (!config || config._retry || !authSessionManager) {
    return false;
  }

  const requestUrl = config.url ?? '';
  const isLoginRequest = requestUrl.includes('/api/v1/auth/login');
  const isRefreshRequest =
    requestUrl.includes('/api/v1/auth/refresh') || requestUrl.includes('/api/v1/auth/refresh-token');
  const isLogoutRequest = requestUrl.includes('/api/v1/auth/logout');

  return !isLoginRequest && !isRefreshRequest && !isLogoutRequest;
}

export function registerAuthSessionManager(sessionManager: AuthSessionManager): void {
  authSessionManager = sessionManager;
}

export function clearAuthSessionManager(): void {
  authSessionManager = null;
}

apiClient.interceptors.request.use((config) => {
  const token = authSessionManager?.getAccessToken();

  if (token) {
    setAuthorizationHeader(config, token);
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const statusCode = error.response?.status;
    const originalConfig = error.config as RetriableRequestConfig | undefined;

    if (statusCode !== 401 || !originalConfig || !shouldTryRefresh(originalConfig) || !authSessionManager) {
      return Promise.reject(error);
    }

    originalConfig._retry = true;

    if (!refreshPromise) {
      refreshPromise = authSessionManager.refreshSession().finally(() => {
        refreshPromise = null;
      });
    }

    const refreshed = await refreshPromise;

    if (refreshed) {
      const updatedToken = authSessionManager.getAccessToken();

      if (updatedToken) {
        setAuthorizationHeader(originalConfig, updatedToken);
      }

      return apiClient(originalConfig);
    }

    await authSessionManager.logout();
    authSessionManager.onSessionExpired?.();

    return Promise.reject(error);
  },
);
