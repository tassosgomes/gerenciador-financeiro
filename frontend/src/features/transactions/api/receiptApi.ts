import { apiClient } from '@/shared/services/apiClient';

import type {
  ImportReceiptRequest,
  ImportReceiptResponse,
  LookupReceiptRequest,
  ReceiptLookupResponse,
  TransactionReceiptResponse,
} from '@/features/transactions/types/receipt';

export async function lookupReceipt(
  request: LookupReceiptRequest,
): Promise<ReceiptLookupResponse> {
  const response = await apiClient.post<ReceiptLookupResponse>('/api/v1/receipts/lookup', request);
  return response.data;
}

export async function importReceipt(
  request: ImportReceiptRequest,
): Promise<ImportReceiptResponse> {
  const response = await apiClient.post<ImportReceiptResponse>('/api/v1/receipts/import', request);
  return response.data;
}

export async function getTransactionReceipt(
  transactionId: string,
): Promise<TransactionReceiptResponse> {
  const response = await apiClient.get<TransactionReceiptResponse>(
    `/api/v1/transactions/${transactionId}/receipt`,
  );
  return response.data;
}
