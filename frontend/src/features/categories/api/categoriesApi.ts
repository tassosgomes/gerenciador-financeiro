import { apiClient } from '@/shared/services/apiClient';
import type {
  CategoryResponse,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '@/features/categories/types/category';
import type { CategoryType } from '@/features/categories/types/category';

export async function getCategories(type?: CategoryType): Promise<CategoryResponse[]> {
  const params = type !== undefined ? { type } : {};
  const response = await apiClient.get<CategoryResponse[]>('/api/v1/categories', { params });
  return response.data;
}

export async function createCategory(data: CreateCategoryRequest): Promise<CategoryResponse> {
  const response = await apiClient.post<CategoryResponse>('/api/v1/categories', data);
  return response.data;
}

export async function updateCategory(
  id: string,
  data: UpdateCategoryRequest
): Promise<CategoryResponse> {
  const response = await apiClient.put<CategoryResponse>(`/api/v1/categories/${id}`, data);
  return response.data;
}
