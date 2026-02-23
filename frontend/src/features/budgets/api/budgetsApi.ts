import { apiClient } from '@/shared/services/apiClient';

import type {
  AvailablePercentageResponse,
  BudgetResponse,
  BudgetSummaryResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from '@/features/budgets/types';

export async function createBudget(data: CreateBudgetRequest): Promise<BudgetResponse> {
  const response = await apiClient.post<BudgetResponse>('/api/v1/budgets', data);
  return response.data;
}

export async function updateBudget(
  id: string,
  data: UpdateBudgetRequest
): Promise<BudgetResponse> {
  const response = await apiClient.put<BudgetResponse>(`/api/v1/budgets/${id}`, data);
  return response.data;
}

export async function deleteBudget(id: string): Promise<void> {
  await apiClient.delete(`/api/v1/budgets/${id}`);
}

export async function getBudgetById(id: string): Promise<BudgetResponse> {
  const response = await apiClient.get<BudgetResponse>(`/api/v1/budgets/${id}`);
  return response.data;
}

export async function listBudgets(month: number, year: number): Promise<BudgetResponse[]> {
  const response = await apiClient.get<BudgetResponse[]>('/api/v1/budgets', {
    params: { month, year },
  });
  return response.data;
}

export async function getBudgetSummary(
  month: number,
  year: number
): Promise<BudgetSummaryResponse> {
  const response = await apiClient.get<BudgetSummaryResponse>('/api/v1/budgets/summary', {
    params: { month, year },
  });
  return response.data;
}

export async function getAvailablePercentage(
  month: number,
  year: number,
  excludeBudgetId?: string
): Promise<AvailablePercentageResponse> {
  const response = await apiClient.get<AvailablePercentageResponse>(
    '/api/v1/budgets/available-percentage',
    {
      params: {
        month,
        year,
        excludeBudgetId,
      },
    }
  );

  return response.data;
}