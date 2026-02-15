import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

import type {
  TransactionResponse,
  CreateTransactionRequest,
  CreateInstallmentRequest,
  CreateRecurrenceRequest,
  CreateTransferRequest,
  AdjustTransactionRequest,
  CancelTransactionRequest,
  TransactionHistoryEntry,
  TransactionFilters,
  PagedResponse,
} from '@/features/transactions/types/transaction';
import {
  getTransactions,
  getTransaction,
  createTransaction,
  createInstallment,
  createRecurrence,
  createTransfer,
  adjustTransaction,
  cancelTransaction,
  getTransactionHistory,
} from '@/features/transactions/api/transactionsApi';

export function useTransactions(filters?: TransactionFilters) {
  return useQuery<PagedResponse<TransactionResponse>>({
    queryKey: ['transactions', filters],
    queryFn: () => getTransactions(filters),
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}

export function useTransaction(id: string) {
  return useQuery<TransactionResponse>({
    queryKey: ['transactions', id],
    queryFn: () => getTransaction(id),
    enabled: !!id,
  });
}

export function useTransactionHistory(id: string) {
  return useQuery<TransactionHistoryEntry[]>({
    queryKey: ['transactions', id, 'history'],
    queryFn: () => getTransactionHistory(id),
    enabled: !!id,
  });
}

export function useCreateTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTransactionRequest) => createTransaction(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      toast.success('Transação criada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao criar transação. Tente novamente.');
    },
  });
}

export function useCreateInstallment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateInstallmentRequest) => createInstallment(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      toast.success('Parcelamento criado com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao criar parcelamento. Tente novamente.');
    },
  });
}

export function useCreateRecurrence() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateRecurrenceRequest) => createRecurrence(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      toast.success('Recorrência criada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao criar recorrência. Tente novamente.');
    },
  });
}

export function useCreateTransfer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTransferRequest) => createTransfer(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      toast.success('Transferência criada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao criar transferência. Tente novamente.');
    },
  });
}

export function useAdjustTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AdjustTransactionRequest }) =>
      adjustTransaction(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      toast.success('Transação ajustada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao ajustar transação. Tente novamente.');
    },
  });
}

export function useCancelTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CancelTransactionRequest }) =>
      cancelTransaction(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      toast.success('Transação cancelada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao cancelar transação. Tente novamente.');
    },
  });
}
