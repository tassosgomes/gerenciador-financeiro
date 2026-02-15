import { useState, useMemo } from 'react';
import { Plus } from 'lucide-react';

import type { CategoryResponse } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';
import { useCategories } from '@/features/categories/hooks/useCategories';
import { CategoryFilter } from '@/features/categories/components/CategoryFilter';
import { CategoryList } from '@/features/categories/components/CategoryList';
import { CategoryForm } from '@/features/categories/components/CategoryForm';
import { Button, Card, CardContent, Skeleton } from '@/shared/components/ui';

type FilterType = 'all' | 'income' | 'expense';

export default function CategoriesPage(): JSX.Element {
  const [filterType, setFilterType] = useState<FilterType>('all');
  const [formOpen, setFormOpen] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<CategoryResponse | null>(null);

  // Determinar o filtro de tipo para a API
  const apiTypeFilter = useMemo(() => {
    if (filterType === 'income') return CategoryType.Income;
    if (filterType === 'expense') return CategoryType.Expense;
    return undefined;
  }, [filterType]);

  const { data: categories = [], isLoading } = useCategories(apiTypeFilter);

  function handleAddCategory(): void {
    setSelectedCategory(null);
    setFormOpen(true);
  }

  function handleEditCategory(category: CategoryResponse): void {
    setSelectedCategory(category);
    setFormOpen(true);
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-slate-900">Categorias</h1>
          <p className="text-slate-600 mt-1">Organize suas receitas e despesas por categorias</p>
        </div>
        <Button onClick={handleAddCategory}>
          <Plus className="mr-2 h-4 w-4" />
          Nova Categoria
        </Button>
      </div>

      {/* Filtros */}
      <Card>
        <CardContent className="pt-6">
          <CategoryFilter value={filterType} onChange={setFilterType} />
        </CardContent>
      </Card>

      {/* Lista de categorias */}
      <Card>
        <CardContent className="pt-6">
          {isLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          ) : (
            <CategoryList categories={categories} onEdit={handleEditCategory} />
          )}
        </CardContent>
      </Card>

      {/* Modal de criação/edição */}
      <CategoryForm open={formOpen} onOpenChange={setFormOpen} category={selectedCategory} />
    </div>
  );
}
