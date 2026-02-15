import { useCallback, useMemo, useState } from 'react';

import type { CategoryResponse } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';
import { createCategorySchema, updateCategorySchema } from '@/features/categories/schemas/categorySchema';
import { useCreateCategory, useUpdateCategory } from '@/features/categories/hooks/useCategories';
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui';

interface CategoryFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  category?: CategoryResponse | null;
}

export function CategoryForm({ open, onOpenChange, category }: CategoryFormProps): JSX.Element {
  const isEditing = !!category;
  const createMutation = useCreateCategory();
  const updateMutation = useUpdateCategory();

  // Initialize state based on mode
  const initialName = useMemo(() => (isEditing && category ? category.name : ''), [isEditing, category]);
  const initialType = useMemo(() => (isEditing && category ? category.type : CategoryType.Expense), [isEditing, category]);

  const [name, setName] = useState(initialName);
  const [selectedType, setSelectedType] = useState<CategoryType>(initialType);
  const [errors, setErrors] = useState<{ name?: string; type?: string }>({});

  const resetForm = useCallback(() => {
    if (isEditing && category) {
      setName(category.name);
      setSelectedType(category.type);
    } else {
      setName('');
      setSelectedType(CategoryType.Expense);
    }
    setErrors({});
  }, [isEditing, category]);

  function handleOpenChange(newOpen: boolean): void {
    if (newOpen) {
      // Reset form when opening
      resetForm();
    }
    onOpenChange(newOpen);
  }

  function validate(): boolean {
    const newErrors: { name?: string; type?: string } = {};
    const schema = isEditing ? updateCategorySchema : createCategorySchema;

    const nameResult = schema.shape.name.safeParse(name);
    if (!nameResult.success) {
      newErrors.name = nameResult.error.issues[0]?.message;
    }

    if (!isEditing) {
      const typeResult = createCategorySchema.shape.type.safeParse(selectedType);
      if (!typeResult.success) {
        newErrors.type = typeResult.error.issues[0]?.message;
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  async function handleSubmit(e: React.FormEvent): Promise<void> {
    e.preventDefault();

    if (!validate()) return;

    try {
      if (isEditing) {
        await updateMutation.mutateAsync({
          id: category.id,
          data: {
            name,
          },
        });
      } else {
        await createMutation.mutateAsync({
          name,
          type: selectedType,
        });
      }
      onOpenChange(false);
    } catch (error) {
      // Error already handled by mutation hook
    }
  }

  const isLoading = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Editar Categoria' : 'Nova Categoria'}</DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Altere o nome da categoria.'
              : 'Preencha os dados para criar uma nova categoria.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Nome */}
          <div className="space-y-2">
            <label htmlFor="name" className="text-sm font-medium">
              Nome <span className="text-red-500">*</span>
            </label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ex: Alimentação"
              className={errors.name ? 'border-red-500' : ''}
              disabled={isLoading}
            />
            {errors.name && <p className="text-sm text-red-500">{errors.name}</p>}
          </div>

          {/* Tipo - apenas em criação */}
          {!isEditing && (
            <div className="space-y-2">
              <label htmlFor="type" className="text-sm font-medium">
                Tipo <span className="text-red-500">*</span>
              </label>
              <Select
                value={String(selectedType)}
                onValueChange={(value) => setSelectedType(Number(value) as CategoryType)}
                disabled={isLoading}
              >
                <SelectTrigger id="type" className={errors.type ? 'border-red-500' : ''}>
                  <SelectValue placeholder="Selecione o tipo" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(CategoryType.Income)}>Receita</SelectItem>
                  <SelectItem value={String(CategoryType.Expense)}>Despesa</SelectItem>
                </SelectContent>
              </Select>
              {errors.type && <p className="text-sm text-red-500">{errors.type}</p>}
            </div>
          )}

          {/* Tipo - somente leitura em edição */}
          {isEditing && (
            <div className="space-y-2">
              <label className="text-sm font-medium">Tipo</label>
              <div className="text-sm text-slate-500 bg-slate-50 p-2 rounded">
                {category.type === CategoryType.Income ? 'Receita' : 'Despesa'}
                <span className="ml-2 text-xs">(não editável)</span>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isLoading}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Salvando...' : isEditing ? 'Salvar' : 'Criar'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
