import { useQuery } from '@tanstack/react-query';

import { getBudgetSummary } from '@/features/budgets/api/budgetsApi';

export function useBudgetSummary(month: number, year: number) {
  return useQuery({
    queryKey: ['budgets', 'summary', year, month],
    queryFn: () => getBudgetSummary(month, year),
  });
}