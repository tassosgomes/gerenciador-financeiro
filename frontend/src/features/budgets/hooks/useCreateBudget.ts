import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

import { createBudget } from '@/features/budgets/api/budgetsApi';
import type { CreateBudgetRequest } from '@/features/budgets/types';
import { getErrorMessage } from '@/shared/utils/errorMessages';

export function useCreateBudget() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBudgetRequest) => createBudget(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['budgets'] });
      queryClient.invalidateQueries({ queryKey: ['budgets', 'summary'] });
      toast.success('Orçamento criado com sucesso');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error));
    },
  });
}