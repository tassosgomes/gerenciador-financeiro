import { useQuery } from '@tanstack/react-query';
import { getDashboardSummary, getDashboardCharts } from '@/features/dashboard/api/dashboardApi';

export function useDashboardSummary(month: number, year: number) {
  return useQuery({
    queryKey: ['dashboard', 'summary', month, year],
    queryFn: () => getDashboardSummary(month, year),
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
}

export function useDashboardCharts(month: number, year: number) {
  return useQuery({
    queryKey: ['dashboard', 'charts', month, year],
    queryFn: () => getDashboardCharts(month, year),
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
}
