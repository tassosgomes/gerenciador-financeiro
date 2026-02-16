import{ apiClient } from '@/shared/services/apiClient';
import type {
  TransactionResponse,
  CreateTransactionRequest,
  CreateInstallmentRequest,
  CreateRecurrenceRequest,
  CreateTransferRequest,
  AdjustTransactionRequest,
  CancelTransactionRequest,
  TransactionHistoryEntry,
  TransactionHistoryResponse,
  TransactionFilters,
  PagedResponse,
} from '@/features/transactions/types/transaction';

export async function getTransactions(
  filters?: TransactionFilters
): Promise<PagedResponse<TransactionResponse>> {
  const params = new URLSearchParams();
  
  if (filters?.accountId) params.append('accountId', filters.accountId);
  if (filters?.categoryId) params.append('categoryId', filters.categoryId);
  if (filters?.type !== undefined) params.append('type', String(filters.type));
  if (filters?.status !== undefined) params.append('status', String(filters.status));
  if (filters?.dateFrom) params.append('dateFrom', filters.dateFrom);
  if (filters?.dateTo) params.append('dateTo', filters.dateTo);
  if (filters?.page !== undefined) params.append('_page', String(filters.page));
  if (filters?.size !== undefined) params.append('_size', String(filters.size));

  const response = await apiClient.get<PagedResponse<TransactionResponse>>(
    `/api/v1/transactions?${params.toString()}`
  );
  return response.data;
}

export async function getTransaction(id: string): Promise<TransactionResponse> {
  const response = await apiClient.get<TransactionResponse>(`/api/v1/transactions/${id}`);
  return response.data;
}

export async function createTransaction(
  data: CreateTransactionRequest
): Promise<TransactionResponse> {
  const response = await apiClient.post<TransactionResponse>('/api/v1/transactions', data);
  return response.data;
}

export async function createInstallment(
  data: CreateInstallmentRequest
): Promise<TransactionResponse[]> {
  const response = await apiClient.post<TransactionResponse[]>(
    '/api/v1/transactions/installments',
    data
  );
  return response.data;
}

export async function createRecurrence(
  data: CreateRecurrenceRequest
): Promise<TransactionResponse> {
  const response = await apiClient.post<TransactionResponse>(
    '/api/v1/transactions/recurrences',
    data
  );
  return response.data;
}

export async function createTransfer(
  data: CreateTransferRequest
): Promise<TransactionResponse[]> {
  const response = await apiClient.post<TransactionResponse[]>(
    '/api/v1/transactions/transfers',
    data
  );
  return response.data;
}

export async function adjustTransaction(
  id: string,
  data: AdjustTransactionRequest
): Promise<TransactionResponse> {
  const response = await apiClient.post<TransactionResponse>(
    `/api/v1/transactions/${id}/adjustments`,
    data
  );
  return response.data;
}

export async function cancelTransaction(
  id: string,
  data: CancelTransactionRequest
): Promise<void> {
  await apiClient.post(`/api/v1/transactions/${id}/cancel`, data);
}

export async function getTransactionHistory(id: string): Promise<TransactionHistoryEntry[]> {
  const response = await apiClient.get<TransactionHistoryResponse>(
    `/api/v1/transactions/${id}/history`
  );
  return response.data.entries || [];
}
