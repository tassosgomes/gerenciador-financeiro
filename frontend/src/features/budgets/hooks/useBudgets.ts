import { useQuery } from '@tanstack/react-query';

import { listBudgets } from '@/features/budgets/api/budgetsApi';

export function useBudgets(month: number, year: number) {
  return useQuery({
    queryKey: ['budgets', year, month],
    queryFn: () => listBudgets(month, year),
  });
}