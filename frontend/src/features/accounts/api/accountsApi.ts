import { apiClient } from '@/shared/services/apiClient';
import type {
  AccountResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
} from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';

type AccountApiResponse = Omit<AccountResponse, 'type'> & {
  type: AccountResponse['type'] | keyof typeof AccountType;
};

function normalizeAccountType(type: AccountApiResponse['type']): AccountResponse['type'] {
  if (typeof type === 'number') {
    return type;
  }

  const normalized = AccountType[type as keyof typeof AccountType];
  return normalized ?? AccountType.Corrente;
}

function normalizeAccount(account: AccountApiResponse): AccountResponse {
  return {
    ...account,
    type: normalizeAccountType(account.type),
  };
}

export async function getAccounts(): Promise<AccountResponse[]> {
  const response = await apiClient.get<AccountApiResponse[]>('/api/v1/accounts');
  return response.data.map(normalizeAccount);
}

export async function getAccount(id: string): Promise<AccountResponse> {
  const response = await apiClient.get<AccountApiResponse>(`/api/v1/accounts/${id}`);
  return normalizeAccount(response.data);
}

export async function createAccount(data: CreateAccountRequest): Promise<AccountResponse> {
  const response = await apiClient.post<AccountApiResponse>('/api/v1/accounts', data);
  return normalizeAccount(response.data);
}

export async function updateAccount(
  id: string,
  data: UpdateAccountRequest
): Promise<AccountResponse> {
  const response = await apiClient.put<AccountApiResponse>(`/api/v1/accounts/${id}`, data);
  return normalizeAccount(response.data);
}

export async function toggleAccountStatus(id: string, isActive: boolean): Promise<void> {
  await apiClient.patch(`/api/v1/accounts/${id}/status`, { isActive });
}
