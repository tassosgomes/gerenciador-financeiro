import type { AuthResponse, LoginRequest } from '@/features/auth/types/auth';
import { apiClient } from '@/shared/services/apiClient';

export async function loginApi(payload: LoginRequest): Promise<AuthResponse> {
  const response = await apiClient.post<AuthResponse>('/api/v1/auth/login', payload);
  return response.data;
}

export async function refreshTokenApi(refreshToken: string): Promise<AuthResponse> {
  const response = await apiClient.post<AuthResponse>('/api/v1/auth/refresh', { refreshToken });
  return response.data;
}

export async function logoutApi(): Promise<void> {
  await apiClient.post('/api/v1/auth/logout');
}
