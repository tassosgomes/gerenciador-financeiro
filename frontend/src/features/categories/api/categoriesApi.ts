import { apiClient } from '@/shared/services/apiClient';
import type {
  CategoryResponse,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';

type CategoryApiResponse = Omit<CategoryResponse, 'type'> & {
  type: CategoryResponse['type'] | keyof typeof CategoryType;
};

function normalizeCategoryType(type: CategoryApiResponse['type']): CategoryResponse['type'] {
  if (typeof type === 'number') {
    return type;
  }

  const normalized = CategoryType[type as keyof typeof CategoryType];
  return normalized ?? CategoryType.Expense;
}

function normalizeCategory(category: CategoryApiResponse): CategoryResponse {
  return {
    ...category,
    type: normalizeCategoryType(category.type),
  };
}

type CategoryTypeFilter = CategoryResponse['type'];

export async function getCategories(type?: CategoryTypeFilter): Promise<CategoryResponse[]> {
  const params = type !== undefined ? { type } : {};
  const response = await apiClient.get<CategoryApiResponse[]>('/api/v1/categories', { params });
  return response.data.map(normalizeCategory);
}

export async function createCategory(data: CreateCategoryRequest): Promise<CategoryResponse> {
  const response = await apiClient.post<CategoryApiResponse>('/api/v1/categories', data);
  return normalizeCategory(response.data);
}

export async function updateCategory(
  id: string,
  data: UpdateCategoryRequest
): Promise<CategoryResponse> {
  const response = await apiClient.put<CategoryApiResponse>(`/api/v1/categories/${id}`, data);
  return normalizeCategory(response.data);
}
