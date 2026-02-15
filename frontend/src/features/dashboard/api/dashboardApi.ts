import type {
  DashboardSummaryResponse,
  DashboardChartsResponse,
} from '@/features/dashboard/types/dashboard';
import { apiClient } from '@/shared/services/apiClient';

export async function getDashboardSummary(
  month: number,
  year: number
): Promise<DashboardSummaryResponse> {
  const response = await apiClient.get<DashboardSummaryResponse>(
    `/api/v1/dashboard/summary?month=${month}&year=${year}`
  );
  return response.data;
}

export async function getDashboardCharts(
  month: number,
  year: number
): Promise<DashboardChartsResponse> {
  const response = await apiClient.get<DashboardChartsResponse>(
    `/api/v1/dashboard/charts?month=${month}&year=${year}`
  );
  return response.data;
}
