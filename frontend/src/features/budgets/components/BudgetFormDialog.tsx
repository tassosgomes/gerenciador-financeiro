import type { BudgetResponse } from '@/features/budgets/types';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/shared/components/ui';

import { BudgetForm } from './BudgetForm';

interface BudgetFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  budget?: BudgetResponse;
  month: number;
  year: number;
}

export function BudgetFormDialog({
  open,
  onOpenChange,
  budget,
  month,
  year,
}: BudgetFormDialogProps): JSX.Element {
  const isEditing = Boolean(budget);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full overflow-y-auto sm:max-w-xl">
        <SheetHeader>
          <SheetTitle>{isEditing ? 'Editar Orçamento' : 'Novo Orçamento'}</SheetTitle>
          <SheetDescription>
            {isEditing
              ? 'Atualize os dados do orçamento selecionado.'
              : 'Defina o percentual da renda e as categorias para criar o orçamento.'}
          </SheetDescription>
        </SheetHeader>

        <div className="mt-6">
          <BudgetForm
            budget={budget}
            month={month}
            year={year}
            onSuccess={() => onOpenChange(false)}
            onCancel={() => onOpenChange(false)}
          />
        </div>
      </SheetContent>
    </Sheet>
  );
}
