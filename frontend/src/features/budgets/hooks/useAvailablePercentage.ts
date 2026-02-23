import { useQuery } from '@tanstack/react-query';

import { getAvailablePercentage } from '@/features/budgets/api/budgetsApi';

export function useAvailablePercentage(
  month: number,
  year: number,
  excludeBudgetId?: string
) {
  return useQuery({
    queryKey: ['budgets', 'available-percentage', year, month, excludeBudgetId],
    queryFn: () => getAvailablePercentage(month, year, excludeBudgetId),
  });
}