import { BudgetDashboard } from '@/features/budgets/components/BudgetDashboard';

export default function BudgetsPage(): JSX.Element {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-slate-900">Orçamentos</h1>
        <p className="mt-1 text-slate-600">
          Acompanhe seus limites por categoria e evite estouros no mês.
        </p>
      </div>

      <BudgetDashboard />
    </div>
  );
}
