import { useState, useMemo } from 'react';
import { Plus } from 'lucide-react';

import type { CategoryResponse } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';
import { useCategories } from '@/features/categories/hooks/useCategories';
import { useDeleteCategory } from '@/features/categories/hooks/useCategories';
import { CategoryFilter } from '@/features/categories/components/CategoryFilter';
import { CategoryList } from '@/features/categories/components/CategoryList';
import { CategoryForm } from '@/features/categories/components/CategoryForm';
import {
  Button,
  CategorySelectOptionGroups,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Select,
  SelectContent,
  SelectTrigger,
  SelectValue,
  Skeleton,
} from '@/shared/components/ui';
import { ConfirmationModal } from '@/shared/components/ui/ConfirmationModal';

type FilterType = 'all' | 'income' | 'expense';

export default function CategoriesPage(): JSX.Element {
  const [filterType, setFilterType] = useState<FilterType>('all');
  const [formOpen, setFormOpen] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<CategoryResponse | null>(null);
  const [categoryToDelete, setCategoryToDelete] = useState<CategoryResponse | null>(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [showMigrationDialog, setShowMigrationDialog] = useState(false);
  const [migrationCategoryId, setMigrationCategoryId] = useState('');

  // Determinar o filtro de tipo para a API
  const apiTypeFilter = useMemo(() => {
    if (filterType === 'income') return CategoryType.Income;
    if (filterType === 'expense') return CategoryType.Expense;
    return undefined;
  }, [filterType]);

  const { data: categories = [], isLoading } = useCategories(apiTypeFilter);
  const deleteCategoryMutation = useDeleteCategory();

  const migrationCandidates = useMemo(() => {
    if (!categoryToDelete) return [];

    return categories.filter(
      (category) =>
        category.id !== categoryToDelete.id
        && category.type === categoryToDelete.type
    );
  }, [categories, categoryToDelete]);

  function handleAddCategory(): void {
    setSelectedCategory(null);
    setFormOpen(true);
  }

  function handleEditCategory(category: CategoryResponse): void {
    setSelectedCategory(category);
    setFormOpen(true);
  }

  function handleDeleteCategory(category: CategoryResponse): void {
    setCategoryToDelete(category);
    setMigrationCategoryId('');
    setShowDeleteConfirm(true);
  }

  async function handleConfirmDelete(): Promise<void> {
    if (!categoryToDelete) return;

    try {
      await deleteCategoryMutation.mutateAsync({ id: categoryToDelete.id });
      setShowDeleteConfirm(false);
      setCategoryToDelete(null);
    } catch (error) {
      const status =
        error && typeof error === 'object' && 'isAxiosError' in error
          ? (error as { response?: { status?: number } }).response?.status
          : undefined;

      if (status === 409) {
        setShowDeleteConfirm(false);
        setMigrationCategoryId('');
        setShowMigrationDialog(true);
      }
    }
  }

  async function handleConfirmMigration(): Promise<void> {
    if (!categoryToDelete || !migrationCategoryId) return;

    await deleteCategoryMutation.mutateAsync({
      id: categoryToDelete.id,
      migrateToCategoryId: migrationCategoryId,
    });

    setShowMigrationDialog(false);
    setMigrationCategoryId('');
    setCategoryToDelete(null);
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-slate-900">Categorias</h1>
          <p className="text-slate-600 mt-1">Organize suas receitas e despesas por categorias</p>
        </div>
        <Button onClick={handleAddCategory} className="shrink-0">
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
            <CategoryList categories={categories} onEdit={handleEditCategory} onDelete={handleDeleteCategory} />
          )}
        </CardContent>
      </Card>

      {/* Modal de criação/edição */}
      <CategoryForm open={formOpen} onOpenChange={setFormOpen} category={selectedCategory} />

      <ConfirmationModal
        open={showDeleteConfirm}
        title="Excluir categoria"
        message={`Tem certeza que deseja excluir a categoria "${categoryToDelete?.name ?? ''}"?`}
        onConfirm={handleConfirmDelete}
        onCancel={() => {
          setShowDeleteConfirm(false);
          setCategoryToDelete(null);
        }}
        onOpenChange={setShowDeleteConfirm}
        confirmLabel="Excluir"
        variant="danger"
      />

      <Dialog open={showMigrationDialog} onOpenChange={setShowMigrationDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Categoria em uso</DialogTitle>
            <DialogDescription>
              Esta categoria possui lançamentos vinculados. Selecione uma categoria de destino para migrar os itens.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-2">
            <label className="text-sm font-medium">Migrar para</label>
            <Select value={migrationCategoryId} onValueChange={setMigrationCategoryId}>
              <SelectTrigger aria-label="Categoria de destino">
                <SelectValue placeholder="Selecione a categoria" />
              </SelectTrigger>
              <SelectContent>
                <CategorySelectOptionGroups
                  items={migrationCandidates}
                  expenseType={CategoryType.Expense}
                  incomeType={CategoryType.Income}
                  includeLeadingSeparator
                />
              </SelectContent>
            </Select>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setShowMigrationDialog(false);
                setMigrationCategoryId('');
                setCategoryToDelete(null);
              }}
            >
              Cancelar
            </Button>
            <Button
              variant="destructive"
              onClick={handleConfirmMigration}
              disabled={!migrationCategoryId || deleteCategoryMutation.isPending}
            >
              Migrar e excluir
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
