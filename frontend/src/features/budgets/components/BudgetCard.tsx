import { AlertCircle, AlertTriangle, Pencil, Trash2 } from 'lucide-react';

import type { BudgetResponse } from '@/features/budgets/types';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui';
import { formatCurrency } from '@/shared/utils';

interface BudgetCardProps {
  budget: BudgetResponse;
  isReadOnly: boolean;
  onEdit?: () => void;
  onDelete?: () => void;
}

function getProgressColor(percentage: number): string {
  if (percentage >= 90) return 'bg-red-500';
  if (percentage >= 70) return 'bg-yellow-500';
  return 'bg-green-500';
}

function normalizePercentage(value: number): number {
  if (!Number.isFinite(value)) return 0;
  return Math.max(0, Math.min(value, 100));
}

export function BudgetCard({ budget, isReadOnly, onEdit, onDelete }: BudgetCardProps): JSX.Element {
  const consumedPercentage = Number(budget.consumedPercentage.toFixed(1));
  const normalizedPercentage = normalizePercentage(consumedPercentage);
  const progressClassName = getProgressColor(consumedPercentage);
  const isNearLimit = consumedPercentage >= 80 && consumedPercentage <= 100;
  const isExceeded = consumedPercentage > 100;

  return (
    <Card className="h-full">
      <CardHeader className="space-y-4">
        <div className="flex items-start justify-between gap-3">
          <div className="space-y-2">
            <CardTitle className="text-lg leading-6">{budget.name} — {budget.percentage}%</CardTitle>
            <div className="flex flex-wrap items-center gap-2">
              {budget.isRecurrent ? <Badge variant="secondary">Recorrente</Badge> : null}
              {isExceeded ? (
                <Badge variant="destructive" className="gap-1">
                  <AlertTriangle className="h-3.5 w-3.5" aria-hidden="true" />
                  Estourado
                </Badge>
              ) : null}
              {isNearLimit ? (
                <span className="inline-flex items-center text-yellow-700" title="Orçamento em alerta (acima de 80%)" aria-label="Orçamento em alerta">
                  <AlertCircle className="h-4 w-4" aria-hidden="true" />
                </span>
              ) : null}
            </div>
          </div>

          {!isReadOnly ? (
            <div className="flex items-center gap-1">
              <Button size="icon" type="button" variant="ghost" onClick={onEdit} aria-label={`Editar orçamento ${budget.name}`}>
                <Pencil className="h-4 w-4" />
              </Button>
              <Button size="icon" type="button" variant="ghost" onClick={onDelete} aria-label={`Excluir orçamento ${budget.name}`}>
                <Trash2 className="h-4 w-4 text-red-600" />
              </Button>
            </div>
          ) : null}
        </div>

        <div className="flex flex-wrap gap-2">
          {budget.categories.map((category) => (
            <Badge key={category.id} variant="outline">
              {category.name}
            </Badge>
          ))}
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="grid grid-cols-3 gap-3 text-sm">
          <div>
            <p className="text-slate-500">Limite</p>
            <p className="font-semibold">{formatCurrency(budget.limitAmount)}</p>
          </div>
          <div>
            <p className="text-slate-500">Consumido</p>
            <p className="font-semibold">{formatCurrency(budget.consumedAmount)}</p>
          </div>
          <div>
            <p className="text-slate-500">Restante</p>
            <p className="font-semibold">{formatCurrency(budget.remainingAmount)}</p>
          </div>
        </div>

        <div className="space-y-2">
          <div
            className="h-3 w-full overflow-hidden rounded-full bg-slate-100"
            role="progressbar"
            aria-label={`Consumo do orçamento ${budget.name}: ${consumedPercentage}%`}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={normalizedPercentage}
          >
            <div
              className={`h-full transition-all ${progressClassName}`}
              style={{ width: `${normalizedPercentage}%` }}
            />
          </div>
          <p className="text-xs text-slate-600">{consumedPercentage}% consumido</p>
        </div>
      </CardContent>
    </Card>
  );
}
