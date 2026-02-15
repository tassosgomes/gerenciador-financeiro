import { Edit } from 'lucide-react';

import type { CategoryResponse } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';
import { Badge, Button, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui';

interface CategoryListProps {
  categories: CategoryResponse[];
  onEdit: (category: CategoryResponse) => void;
}

export function CategoryList({ categories, onEdit }: CategoryListProps): JSX.Element {
  if (categories.length === 0) {
    return (
      <div className="text-center py-12 text-slate-500">
        <p className="text-lg font-medium">Nenhuma categoria encontrada</p>
        <p className="text-sm mt-2">Crie sua primeira categoria para começar</p>
      </div>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Nome</TableHead>
          <TableHead>Tipo</TableHead>
          <TableHead className="w-24">Ações</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {categories.map((category) => (
          <TableRow key={category.id}>
            <TableCell className="font-medium">{category.name}</TableCell>
            <TableCell>
              {category.type === CategoryType.Income ? (
                <Badge className="bg-green-100 text-green-800 hover:bg-green-100">
                  Receita
                </Badge>
              ) : (
                <Badge className="bg-red-100 text-red-800 hover:bg-red-100">
                  Despesa
                </Badge>
              )}
            </TableCell>
            <TableCell>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => onEdit(category)}
                aria-label={`Editar ${category.name}`}
              >
                <Edit className="h-4 w-4" />
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
