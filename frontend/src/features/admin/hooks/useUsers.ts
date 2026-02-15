import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

import type { CreateUserRequest, UserResponse } from '@/features/admin/types/admin';
import { createUser, getUsers, toggleUserStatus } from '@/features/admin/api/usersApi';

export function useUsers() {
  return useQuery<UserResponse[]>({
    queryKey: ['users'],
    queryFn: getUsers,
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateUserRequest) => createUser(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('Usuário criado com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao criar usuário. Tente novamente.');
    },
  });
}

export function useToggleUserStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      toggleUserStatus(id, isActive),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      const action = variables.isActive ? 'ativado' : 'inativado';
      toast.success(`Usuário ${action} com sucesso!`);
    },
    onError: () => {
      toast.error('Erro ao alterar status do usuário. Tente novamente.');
    },
  });
}
