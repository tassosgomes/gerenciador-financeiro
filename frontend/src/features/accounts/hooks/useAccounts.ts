import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

import type {
  AccountResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
} from '@/features/accounts/types/account';
import {
  createAccount,
  getAccount,
  getAccounts,
  toggleAccountStatus,
  updateAccount,
} from '@/features/accounts/api/accountsApi';

export function useAccounts() {
  return useQuery<AccountResponse[]>({
    queryKey: ['accounts'],
    queryFn: getAccounts,
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}

export function useAccount(id: string) {
  return useQuery<AccountResponse>({
    queryKey: ['accounts', id],
    queryFn: () => getAccount(id),
    enabled: !!id,
  });
}

export function useCreateAccount() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateAccountRequest) => createAccount(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      toast.success('Conta criada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao criar conta. Tente novamente.');
    },
  });
}

export function useUpdateAccount() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAccountRequest }) => updateAccount(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      toast.success('Conta atualizada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao atualizar conta. Tente novamente.');
    },
  });
}

export function useToggleAccountStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      toggleAccountStatus(id, isActive),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      const action = variables.isActive ? 'ativada' : 'inativada';
      toast.success(`Conta ${action} com sucesso!`);
    },
    onError: () => {
      toast.error('Erro ao alterar status da conta. Tente novamente.');
    },
  });
}
