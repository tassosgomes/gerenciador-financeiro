import{ apiClient } from '@/shared/services/apiClient';
import type {
  TransactionResponse,
  CreateTransactionRequest,
  CreateInstallmentRequest,
  CreateRecurrenceRequest,
  CreateTransferRequest,
  AdjustTransactionRequest,
  CancelTransactionRequest,
  MarkTransactionPaidRequest,
  DeactivateRecurrenceRequest,
  TransactionHistoryEntry,
  TransactionHistoryResponse,
  TransactionFilters,
  PagedResponse,
} from '@/features/transactions/types/transaction';
import { TransactionStatus, TransactionType } from '@/features/transactions/types/transaction';

type RawEnumValue = number | string | null | undefined;

type RawTransactionHistoryEntry = {
  id?: string;
  transactionId?: string;
  action?: string;
  performedBy?: string;
  performedAt?: string;
  details?: string | null;
  transaction?: TransactionResponse;
  actionType?: string;
};

type AuthSnapshot = {
  user?: {
    id?: string;
    email?: string;
  };
};

function getAuthSnapshot(): AuthSnapshot | null {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const raw = window.localStorage.getItem('gf.auth');
    return raw ? (JSON.parse(raw) as AuthSnapshot) : null;
  } catch {
    return null;
  }
}

function getCurrentUserLogin(): string | null {
  const auth = getAuthSnapshot();
  return auth?.user?.email ?? null;
}

function resolveUserLoginFromId(userId?: string | null): string | null {
  if (!userId) {
    return null;
  }

  const auth = getAuthSnapshot();
  if (!auth?.user?.id || !auth.user.email) {
    return null;
  }

  return auth.user.id === userId ? auth.user.email : null;
}

function parseTransactionType(value: RawEnumValue): TransactionType {
  if (value === TransactionType.Credit || String(value).toLowerCase() === 'credit') {
    return TransactionType.Credit;
  }

  return TransactionType.Debit;
}

function parseTransactionStatus(value: RawEnumValue): TransactionStatus {
  const normalized = String(value ?? '').toLowerCase();

  if (value === TransactionStatus.Paid || normalized === 'paid') {
    return TransactionStatus.Paid;
  }

  if (
    value === TransactionStatus.Cancelled ||
    normalized === 'cancelled' ||
    normalized === 'canceled'
  ) {
    return TransactionStatus.Cancelled;
  }

  return TransactionStatus.Pending;
}

function toApiTransactionType(value: TransactionType): 'Debit' | 'Credit' {
  return value === TransactionType.Credit ? 'Credit' : 'Debit';
}

function toApiTransactionStatus(value: TransactionStatus): 'Paid' | 'Pending' | 'Cancelled' {
  if (value === TransactionStatus.Paid) {
    return 'Paid';
  }

  if (value === TransactionStatus.Cancelled) {
    return 'Cancelled';
  }

  return 'Pending';
}

function normalizeTransaction(transaction: TransactionResponse): TransactionResponse {
  return {
    ...transaction,
    type: parseTransactionType(transaction.type as RawEnumValue),
    status: parseTransactionStatus(transaction.status as RawEnumValue),
  };
}

function mapActionTypeToAction(actionType?: string): string {
  switch (actionType) {
    case 'Cancellation':
      return 'Cancelled';
    case 'Adjustment':
      return 'Adjusted';
    case 'Original':
      return 'Created';
    default:
      return actionType || 'Updated';
  }
}

function mapHistoryEntry(entry: RawTransactionHistoryEntry, index: number): TransactionHistoryEntry {
  if (entry.transaction) {
    const transaction = normalizeTransaction(entry.transaction);
    const action = mapActionTypeToAction(entry.actionType);

    return {
      id: `${transaction.id}-${action}-${index}`,
      transactionId: transaction.id,
      action,
      performedBy:
        entry.performedBy ??
        resolveUserLoginFromId(transaction.cancelledBy) ??
        getCurrentUserLogin() ??
        transaction.cancelledBy ??
        'Sistema',
      performedAt:
        entry.performedAt ??
        transaction.cancelledAt ??
        transaction.updatedAt ??
        transaction.createdAt,
      details:
        entry.details ??
        (transaction.cancellationReason ||
          (action === 'Cancelled' ? 'Transação cancelada' : null)),
    };
  }

  return {
    id: entry.id ?? `${entry.transactionId ?? 'history'}-${index}`,
    transactionId: entry.transactionId ?? 'unknown',
    action: entry.action ?? mapActionTypeToAction(entry.actionType),
    performedBy: entry.performedBy ?? 'Sistema',
    performedAt: entry.performedAt ?? new Date(0).toISOString(),
    details: entry.details ?? null,
  };
}

export async function getTransactions(
  filters?: TransactionFilters
): Promise<PagedResponse<TransactionResponse>> {
  const params = new URLSearchParams();
  
  if (filters?.accountId) params.append('accountId', filters.accountId);
  if (filters?.categoryId) params.append('categoryId', filters.categoryId);
  if (filters?.type !== undefined) params.append('type', String(filters.type));
  if (filters?.status !== undefined) params.append('status', String(filters.status));
  if (filters?.dateFrom) params.append('competenceDateFrom', filters.dateFrom);
  if (filters?.dateTo) params.append('competenceDateTo', filters.dateTo);
  if (filters?.page !== undefined) params.append('_page', String(filters.page));
  if (filters?.size !== undefined) params.append('_size', String(filters.size));

  const response = await apiClient.get<PagedResponse<TransactionResponse>>(
    `/api/v1/transactions?${params.toString()}`
  );

  return {
    ...response.data,
    data: response.data.data.map(normalizeTransaction),
  };
}

export async function getTransaction(id: string): Promise<TransactionResponse> {
  const response = await apiClient.get<TransactionResponse>(`/api/v1/transactions/${id}`);
  return normalizeTransaction(response.data);
}

export async function createTransaction(
  data: CreateTransactionRequest
): Promise<TransactionResponse> {
  const response = await apiClient.post<TransactionResponse>('/api/v1/transactions', {
    ...data,
    type: toApiTransactionType(data.type),
    status: toApiTransactionStatus(data.status),
  });
  return normalizeTransaction(response.data);
}

export async function createInstallment(
  data: CreateInstallmentRequest
): Promise<TransactionResponse[]> {
  const response = await apiClient.post<TransactionResponse[]>(
    '/api/v1/transactions/installments',
    {
      accountId: data.accountId,
      categoryId: data.categoryId,
      type: toApiTransactionType(data.type),
      amount: data.totalAmount,
      numberOfInstallments: data.installmentCount,
      description: data.description,
      competenceDate: data.firstCompetenceDate,
      dueDate: data.firstDueDate ?? data.firstCompetenceDate,
      operationId: data.operationId,
    }
  );
  return response.data.map(normalizeTransaction);
}

export async function createRecurrence(
  data: CreateRecurrenceRequest
): Promise<TransactionResponse> {
  const response = await apiClient.post<TransactionResponse>(
    '/api/v1/transactions/recurrences',
    {
      ...data,
      type: toApiTransactionType(data.type),
      defaultStatus:
        data.defaultStatus !== undefined
          ? toApiTransactionStatus(data.defaultStatus)
          : undefined,
    }
  );
  return normalizeTransaction(response.data);
}

export async function createTransfer(
  data: CreateTransferRequest
): Promise<TransactionResponse[]> {
  const response = await apiClient.post<TransactionResponse[]>(
    '/api/v1/transactions/transfers',
    data
  );
  return response.data.map(normalizeTransaction);
}

export async function adjustTransaction(
  id: string,
  data: AdjustTransactionRequest
): Promise<TransactionResponse> {
  const response = await apiClient.post<TransactionResponse>(
    `/api/v1/transactions/${id}/adjustments`,
    data
  );
  return normalizeTransaction(response.data);
}

export async function cancelTransaction(
  id: string,
  data: CancelTransactionRequest
): Promise<void> {
  await apiClient.post(`/api/v1/transactions/${id}/cancel`, data);
}

export async function markTransactionAsPaid(
  id: string,
  data?: MarkTransactionPaidRequest
): Promise<TransactionResponse> {
  const response = await apiClient.post<TransactionResponse>(`/api/v1/transactions/${id}/pay`, data ?? {});
  return normalizeTransaction(response.data);
}

export async function deactivateRecurrence(
  recurrenceTemplateId: string,
  data?: DeactivateRecurrenceRequest
): Promise<void> {
  await apiClient.post(`/api/v1/transactions/recurrences/${recurrenceTemplateId}/deactivate`, data ?? {});
}

export async function getTransactionHistory(id: string): Promise<TransactionHistoryEntry[]> {
  const response = await apiClient.get<TransactionHistoryResponse | RawTransactionHistoryEntry[]>(
    `/api/v1/transactions/${id}/history`
  );

  const payload = response.data;
  const entries = Array.isArray(payload) ? payload : payload.entries || [];

  return entries.map((entry, index) => mapHistoryEntry(entry as RawTransactionHistoryEntry, index));
}
