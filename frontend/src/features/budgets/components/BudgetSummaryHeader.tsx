import { AlertTriangle } from 'lucide-react';

import type { BudgetSummaryResponse } from '@/features/budgets/types';
import { Card, CardContent } from '@/shared/components/ui';
import { formatCurrency } from '@/shared/utils';

interface BudgetSummaryHeaderProps {
  summary: BudgetSummaryResponse;
}

export function BudgetSummaryHeader({ summary }: BudgetSummaryHeaderProps): JSX.Element {
  const remainingClassName = summary.totalRemainingAmount >= 0 ? 'text-green-700' : 'text-red-700';

  return (
    <div className="grid grid-cols-2 gap-4 xl:grid-cols-6">
      <Card>
        <CardContent className="pt-6">
          <p className="text-xs font-medium text-slate-500">Renda Mensal</p>
          <p className="text-lg font-bold">{formatCurrency(summary.monthlyIncome)}</p>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <p className="text-xs font-medium text-slate-500">Total Orçado</p>
          <p className="text-lg font-bold">{formatCurrency(summary.totalBudgetedAmount)}</p>
          <p className="text-xs text-slate-500">{summary.totalBudgetedPercentage.toFixed(1)}%</p>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <p className="text-xs font-medium text-slate-500">Total Gasto</p>
          <p className="text-lg font-bold">{formatCurrency(summary.totalConsumedAmount)}</p>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <p className="text-xs font-medium text-slate-500">Saldo Restante</p>
          <p className={`text-lg font-bold ${remainingClassName}`}>{formatCurrency(summary.totalRemainingAmount)}</p>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <p className="text-xs font-medium text-slate-500">Renda Não Orçada</p>
          <p className="text-lg font-bold">{formatCurrency(summary.unbudgetedAmount)}</p>
          <p className="text-xs text-slate-500">{summary.unbudgetedPercentage.toFixed(1)}%</p>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <p className="text-xs font-medium text-slate-500">Gastos Fora de Orçamento</p>
          <div className="flex items-center gap-2">
            <p className="text-lg font-bold">{formatCurrency(summary.unbudgetedExpenses)}</p>
            {summary.unbudgetedExpenses > 0 ? (
              <span title="Existem gastos sem orçamento" aria-label="Atenção: gastos fora de orçamento">
                <AlertTriangle className="h-4 w-4 text-yellow-600" aria-hidden="true" />
              </span>
            ) : null}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
