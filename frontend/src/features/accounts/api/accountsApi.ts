import { apiClient } from '@/shared/services/apiClient';
import type {
  AccountResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
} from '@/features/accounts/types/account';

export async function getAccounts(): Promise<AccountResponse[]> {
  const response = await apiClient.get<AccountResponse[]>('/api/v1/accounts');
  return response.data;
}

export async function getAccount(id: string): Promise<AccountResponse> {
  const response = await apiClient.get<AccountResponse>(`/api/v1/accounts/${id}`);
  return response.data;
}

export async function createAccount(data: CreateAccountRequest): Promise<AccountResponse> {
  const response = await apiClient.post<AccountResponse>('/api/v1/accounts', data);
  return response.data;
}

export async function updateAccount(
  id: string,
  data: UpdateAccountRequest
): Promise<AccountResponse> {
  const response = await apiClient.put<AccountResponse>(`/api/v1/accounts/${id}`, data);
  return response.data;
}

export async function toggleAccountStatus(id: string, isActive: boolean): Promise<void> {
  await apiClient.patch(`/api/v1/accounts/${id}/status`, { isActive });
}
