import { Edit, FolderOpen, Lock, Trash2 } from 'lucide-react';

import type { CategoryResponse } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';
import { Badge, Button, EmptyState, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui';

interface CategoryListProps {
  categories: CategoryResponse[];
  onEdit: (category: CategoryResponse) => void;
  onDelete: (category: CategoryResponse) => void;
}

export function CategoryList({ categories, onEdit, onDelete }: CategoryListProps): JSX.Element {
  if (categories.length === 0) {
    return (
      <EmptyState
        icon={FolderOpen}
        title="Nenhuma categoria encontrada"
        description="Crie sua primeira categoria para organizar suas transações"
      />
    );
  }

  return (
    <div className="w-full overflow-x-auto">
      <Table className="min-w-[480px]">
        <TableHeader>
          <TableRow>
            <TableHead className="min-w-[200px]">Nome</TableHead>
            <TableHead className="min-w-[120px]">Tipo</TableHead>
            <TableHead className="w-24">Ações</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {categories.map((category) => (
            <TableRow key={category.id}>
              <TableCell className="font-medium">
                <div className="flex items-center gap-2">
                  {category.name}
                  {category.isSystem && (
                    <Lock className="h-3 w-3 text-slate-400" aria-label="Categoria do sistema" />
                  )}
                </div>
              </TableCell>
              <TableCell>
                {category.type === CategoryType.Expense ? (
                  <Badge className="bg-red-100 text-red-800 hover:bg-red-100">
                    Despesa
                  </Badge>
                ) : (
                  <Badge className="bg-green-100 text-green-800 hover:bg-green-100">
                    Receita
                  </Badge>
                )}
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => onEdit(category)}
                    disabled={category.isSystem}
                    aria-label={
                      category.isSystem
                        ? `Categoria ${category.name} do sistema não pode ser editada`
                        : `Editar categoria ${category.name}`
                    }
                    className={category.isSystem ? 'cursor-not-allowed opacity-40' : ''}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>

                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => onDelete(category)}
                    disabled={category.isSystem}
                    aria-label={
                      category.isSystem
                        ? `Categoria ${category.name} do sistema não pode ser removida`
                        : `Excluir categoria ${category.name}`
                    }
                    className={category.isSystem ? 'cursor-not-allowed opacity-40' : ''}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

