import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

import { updateBudget } from '@/features/budgets/api/budgetsApi';
import type { UpdateBudgetRequest } from '@/features/budgets/types';
import { getErrorMessage } from '@/shared/utils/errorMessages';

export function useUpdateBudget() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBudgetRequest }) =>
      updateBudget(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['budgets'] });
      queryClient.invalidateQueries({ queryKey: ['budgets', 'summary'] });
      toast.success('Orçamento atualizado com sucesso');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error));
    },
  });
}