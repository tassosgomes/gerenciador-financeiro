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
  // Se já é um número válido, retorna diretamente
  if (typeof type === 'number') {
    // Valida se é um valor válido do enum
    if (type === CategoryType.Income || type === CategoryType.Expense) {
      return type;
    }
  }

  // Se é uma string, tenta converter pela chave
  if (typeof type === 'string') {
    const normalizedString = type.trim().toLowerCase();

    if (normalizedString === 'receita' || normalizedString === 'income') {
      return CategoryType.Income;
    }

    if (normalizedString === 'despesa' || normalizedString === 'expense') {
      return CategoryType.Expense;
    }

    const normalized = CategoryType[type as keyof typeof CategoryType];
    if (normalized !== undefined) {
      return normalized;
    }
  }

  // Fallback para Expense em caso de valor inválido
  return CategoryType.Expense;
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

export async function deleteCategory(id: string, migrateToCategoryId?: string): Promise<void> {
  const params = migrateToCategoryId ? { migrateToCategoryId } : undefined;
  await apiClient.delete(`/api/v1/categories/${id}`, { params });
}
