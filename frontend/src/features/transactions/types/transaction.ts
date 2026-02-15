export const TransactionType = {
  Debit: 1,
  Credit: 2,
} as const;

export type TransactionType = (typeof TransactionType)[keyof typeof TransactionType];

export const TransactionStatus = {
  Paid: 1,
  Pending: 2,
  Cancelled: 3,
} as const;

export type TransactionStatus = (typeof TransactionStatus)[keyof typeof TransactionStatus];

export interface TransactionResponse {
  id: string;
  accountId: string;
  categoryId: string;
  type: TransactionType;
  amount: number;
  description: string;
  competenceDate: string;
  dueDate: string | null;
  status: TransactionStatus;
  isAdjustment: boolean;
  originalTransactionId: string | null;
  hasAdjustment: boolean;
  installmentGroupId: string | null;
  installmentNumber: number | null;
  totalInstallments: number | null;
  isRecurrent: boolean;
  recurrenceTemplateId: string | null;
  transferGroupId: string | null;
  cancellationReason: string | null;
  cancelledBy: string | null;
  cancelledAt: string | null;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateTransactionRequest {
  accountId: string;
  categoryId: string;
  type: TransactionType;
  amount: number;
  description: string;
  competenceDate: string;
  dueDate?: string;
  status: TransactionStatus;
  operationId?: string;
}

export interface CreateInstallmentRequest {
  accountId: string;
  categoryId: string;
  type: TransactionType;
  totalAmount: number;
  installmentCount: number;
  description: string;
  firstCompetenceDate: string;
  firstDueDate?: string;
  operationId?: string;
}

export interface CreateRecurrenceRequest {
  accountId: string;
  categoryId: string;
  type: TransactionType;
  amount: number;
  description: string;
  startDate: string;
  dueDate?: string;
  operationId?: string;
}

export interface CreateTransferRequest {
  sourceAccountId: string;
  destinationAccountId: string;
  categoryId: string;
  amount: number;
  description: string;
  competenceDate: string;
  operationId?: string;
}

export interface AdjustTransactionRequest {
  newAmount: number;
  justification: string;
  operationId?: string;
}

export interface CancelTransactionRequest {
  reason?: string;
  operationId?: string;
}

export interface TransactionHistoryEntry {
  id: string;
  transactionId: string;
  action: string;
  performedBy: string;
  performedAt: string;
  details: string | null;
}

export interface TransactionFilters {
  accountId?: string;
  categoryId?: string;
  type?: TransactionType;
  status?: TransactionStatus;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  size?: number;
}

export interface PagedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    size: number;
    total: number;
    totalPages: number;
  };
}
