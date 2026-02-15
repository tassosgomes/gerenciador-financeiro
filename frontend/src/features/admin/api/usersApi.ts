import { apiClient } from '@/shared/services/apiClient';
import type { CreateUserRequest, UserResponse } from '@/features/admin/types/admin';

export async function getUsers(): Promise<UserResponse[]> {
  const response = await apiClient.get<UserResponse[]>('/api/v1/users');
  return response.data;
}

export async function createUser(data: CreateUserRequest): Promise<UserResponse> {
  const response = await apiClient.post<UserResponse>('/api/v1/users', data);
  return response.data;
}

export async function toggleUserStatus(id: string, isActive: boolean): Promise<void> {
  await apiClient.patch(`/api/v1/users/${id}/status`, { isActive });
}
