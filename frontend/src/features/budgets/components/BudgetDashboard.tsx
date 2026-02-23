import { useMemo, useState } from 'react';
import { PiggyBank, Plus, Wallet } from 'lucide-react';

import { useAvailablePercentage, useBudgetSummary, useDeleteBudget } from '@/features/budgets';
import type { BudgetResponse } from '@/features/budgets/types';
import { BudgetCard } from '@/features/budgets/components/BudgetCard';
import { BudgetFormDialog } from '@/features/budgets/components/BudgetFormDialog';
import { BudgetSummaryHeader } from '@/features/budgets/components/BudgetSummaryHeader';
import { MonthYearFilter } from '@/features/budgets/components/MonthYearFilter';
import { Button, ConfirmationModal, EmptyState, Skeleton } from '@/shared/components/ui';

function isPastMonth(month: number, year: number): boolean {
  const selected = new Date(year, month - 1, 1);
  const current = new Date();
  const normalizedCurrent = new Date(current.getFullYear(), current.getMonth(), 1);

  return selected < normalizedCurrent;
}

export function BudgetDashboard(): JSX.Element {
  const now = new Date();
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [year, setYear] = useState(now.getFullYear());

  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [editingBudget, setEditingBudget] = useState<BudgetResponse | undefined>();
  const [budgetToDelete, setBudgetToDelete] = useState<BudgetResponse | undefined>();

  const { data: summary, isLoading, isError } = useBudgetSummary(month, year);
  const { data: available } = useAvailablePercentage(month, year);
  const deleteBudgetMutation = useDeleteBudget();

  const readOnly = useMemo(() => isPastMonth(month, year), [month, year]);
  const availablePercentage = available?.availablePercentage ?? 0;
  const canCreate = !readOnly && availablePercentage > 0;

  const handleMonthYearChange = (nextMonth: number, nextYear: number): void => {
    setMonth(nextMonth);
    setYear(nextYear);
    setIsCreateDialogOpen(false);
    setEditingBudget(undefined);
    setBudgetToDelete(undefined);
  };

  const handleDeleteBudget = async (): Promise<void> => {
    if (!budgetToDelete) {
      return;
    }

    await deleteBudgetMutation.mutateAsync(budgetToDelete.id);
    setBudgetToDelete(undefined);
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <MonthYearFilter month={month} year={year} onChange={handleMonthYearChange} />
        <div className="grid grid-cols-2 gap-4 xl:grid-cols-6">
          {Array.from({ length: 6 }).map((_, index) => (
            <Skeleton key={index} className="h-24 w-full" />
          ))}
        </div>
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-64 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (isError || !summary) {
    return (
      <div className="space-y-6">
        <MonthYearFilter month={month} year={year} onChange={handleMonthYearChange} />
        <EmptyState
          icon={PiggyBank}
          title="Não foi possível carregar os orçamentos"
          description="Tente novamente em alguns instantes."
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <MonthYearFilter month={month} year={year} onChange={handleMonthYearChange} />
        {canCreate ? (
          <Button onClick={() => setIsCreateDialogOpen(true)} type="button">
            <Plus className="mr-2 h-4 w-4" />
            Novo Orçamento
          </Button>
        ) : null}
      </div>

      <BudgetSummaryHeader summary={summary} />

      {summary.budgets.length === 0 ? (
        <EmptyState
          icon={Wallet}
          title="Nenhum orçamento criado para este mês"
          description={
            readOnly
              ? 'Este mês está em modo somente leitura e não possui orçamentos cadastrados.'
              : 'Crie o primeiro orçamento para começar a acompanhar seus limites de gastos.'
          }
          actionLabel={canCreate ? 'Novo Orçamento' : undefined}
          onAction={canCreate ? () => setIsCreateDialogOpen(true) : undefined}
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          {summary.budgets.map((budget) => (
            <BudgetCard
              key={budget.id}
              budget={budget}
              isReadOnly={readOnly}
              onEdit={() => setEditingBudget(budget)}
              onDelete={() => setBudgetToDelete(budget)}
            />
          ))}
        </div>
      )}

      <BudgetFormDialog
        open={isCreateDialogOpen || Boolean(editingBudget)}
        onOpenChange={(open) => {
          if (!open) {
            setIsCreateDialogOpen(false);
            setEditingBudget(undefined);
          }
        }}
        budget={editingBudget}
        month={month}
        year={year}
      />

      <ConfirmationModal
        open={Boolean(budgetToDelete)}
        onOpenChange={(open) => {
          if (!open) {
            setBudgetToDelete(undefined);
          }
        }}
        title="Excluir orçamento"
        message={`Tem certeza que deseja excluir o orçamento \"${budgetToDelete?.name ?? ''}\"?`}
        onCancel={() => setBudgetToDelete(undefined)}
        onConfirm={() => {
          void handleDeleteBudget();
        }}
        confirmLabel="Excluir"
        variant="danger"
      />
    </div>
  );
}
