import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

import type {
  CategoryResponse,
  CategoryType,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '@/features/categories/types/category';
import {
  createCategory,
  getCategories,
  updateCategory,
} from '@/features/categories/api/categoriesApi';

export function useCategories(type?: CategoryType) {
  return useQuery<CategoryResponse[]>({
    queryKey: ['categories', type],
    queryFn: () => getCategories(type),
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}

export function useCreateCategory() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateCategoryRequest) => createCategory(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
      toast.success('Categoria criada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao criar categoria. Tente novamente.');
    },
  });
}

export function useUpdateCategory() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCategoryRequest }) =>
      updateCategory(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] });
      toast.success('Categoria atualizada com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao atualizar categoria. Tente novamente.');
    },
  });
}
