import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

import { deleteBudget } from '@/features/budgets/api/budgetsApi';
import { getErrorMessage } from '@/shared/utils/errorMessages';

export function useDeleteBudget() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteBudget(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['budgets'] });
      queryClient.invalidateQueries({ queryKey: ['budgets', 'summary'] });
      toast.success('Orçamento excluído com sucesso');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error));
    },
  });
}