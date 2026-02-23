import { useQuery } from '@tanstack/react-query';

import { getTransactionReceipt } from '@/features/transactions/api/receiptApi';
import type { TransactionReceiptResponse } from '@/features/transactions/types/receipt';

export function useTransactionReceipt(transactionId?: string, hasReceipt = false) {
  const isEnabled = Boolean(transactionId) && hasReceipt;

  return useQuery<TransactionReceiptResponse>({
    queryKey: ['transactions', transactionId, 'receipt'],
    queryFn: () => getTransactionReceipt(transactionId as string),
    enabled: isEnabled,
  });
}
