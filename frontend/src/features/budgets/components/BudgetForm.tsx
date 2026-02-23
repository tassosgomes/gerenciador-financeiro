import { useEffect, useMemo, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2 } from 'lucide-react';
import { useForm } from 'react-hook-form';

import { useCategories } from '@/features/categories/hooks/useCategories';
import { CategoryType } from '@/features/categories/types/category';
import { budgetSchema, type BudgetFormData, useAvailablePercentage, useBudgetSummary, useBudgets, useCreateBudget, useUpdateBudget } from '@/features/budgets';
import type { BudgetResponse, CreateBudgetRequest, UpdateBudgetRequest } from '@/features/budgets/types';
import {
  Badge,
  Button,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
} from '@/shared/components/ui';
import { getErrorMessage } from '@/shared/utils/errorMessages';
import { formatCurrency, formatCompetenceMonth } from '@/shared/utils';

interface BudgetFormProps {
  budget?: BudgetResponse;
  month: number;
  year: number;
  onSuccess: () => void;
  onCancel: () => void;
}

const MONTHS = [
  { value: 1, label: 'Janeiro' },
  { value: 2, label: 'Fevereiro' },
  { value: 3, label: 'Março' },
  { value: 4, label: 'Abril' },
  { value: 5, label: 'Maio' },
  { value: 6, label: 'Junho' },
  { value: 7, label: 'Julho' },
  { value: 8, label: 'Agosto' },
  { value: 9, label: 'Setembro' },
  { value: 10, label: 'Outubro' },
  { value: 11, label: 'Novembro' },
  { value: 12, label: 'Dezembro' },
] as const;

export function BudgetForm({ budget, month, year, onSuccess, onCancel }: BudgetFormProps): JSX.Element {
  const isEditing = Boolean(budget);
  const [referenceMonth, setReferenceMonth] = useState(month);
  const [referenceYear, setReferenceYear] = useState(year);
  const [apiError, setApiError] = useState<string | null>(null);
  const [percentageError, setPercentageError] = useState<string | null>(null);

  const form = useForm<BudgetFormData>({
    resolver: zodResolver(budgetSchema),
    defaultValues: {
      name: budget?.name ?? '',
      percentage: budget?.percentage ?? 0,
      referenceMonth: budget?.referenceMonth ?? month,
      referenceYear: budget?.referenceYear ?? year,
      categoryIds: budget?.categories.map((category) => category.id) ?? [],
      isRecurrent: budget?.isRecurrent ?? false,
    },
  });

  const { data: categories = [] } = useCategories(CategoryType.Expense);
  const { data: monthlyBudgets = [] } = useBudgets(referenceMonth, referenceYear);
  const { data: availableData, isLoading: isLoadingAvailable } = useAvailablePercentage(
    referenceMonth,
    referenceYear,
    budget?.id
  );
  const { data: summary } = useBudgetSummary(referenceMonth, referenceYear);

  const createMutation = useCreateBudget();
  const updateMutation = useUpdateBudget();

  useEffect(() => {
    setReferenceMonth(budget?.referenceMonth ?? month);
    setReferenceYear(budget?.referenceYear ?? year);

    form.reset({
      name: budget?.name ?? '',
      percentage: budget?.percentage ?? 0,
      referenceMonth: budget?.referenceMonth ?? month,
      referenceYear: budget?.referenceYear ?? year,
      categoryIds: budget?.categories.map((category) => category.id) ?? [],
      isRecurrent: budget?.isRecurrent ?? false,
    });
    setApiError(null);
    setPercentageError(null);
  }, [budget, month, year, form]);

  const selectedCategoryIds = form.watch('categoryIds');
  const selectedPercentage = form.watch('percentage');

  const availablePercentage = availableData?.availablePercentage ?? 0;
  const monthlyIncome = summary?.monthlyIncome ?? budget?.monthlyIncome ?? 0;

  const categoryBudgetMap = useMemo(() => {
    const map = new Map<string, string>();

    monthlyBudgets
      .filter((monthlyBudget) => monthlyBudget.id !== budget?.id)
      .forEach((monthlyBudget) => {
        monthlyBudget.categories.forEach((category) => {
          map.set(category.id, monthlyBudget.name);
        });
      });

    return map;
  }, [monthlyBudgets, budget?.id]);

  const years = useMemo(() => {
    const currentYear = new Date().getFullYear();
    return Array.from({ length: currentYear + 1 - 2020 + 1 }, (_, index) => 2020 + index);
  }, []);

  const projectedAmount = monthlyIncome > 0 ? monthlyIncome * ((selectedPercentage || 0) / 100) : 0;

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const toggleCategory = (categoryId: string): void => {
    const current = form.getValues('categoryIds');
    const exists = current.includes(categoryId);

    const nextValue = exists
      ? current.filter((currentCategoryId) => currentCategoryId !== categoryId)
      : [...current, categoryId];

    form.setValue('categoryIds', nextValue);
  };

  async function handleSubmit(data: BudgetFormData): Promise<void> {
    setApiError(null);
    setPercentageError(null);

    if (data.percentage > availablePercentage) {
      setPercentageError(`Percentual excede o disponível para o mês (${availablePercentage.toFixed(2)}%)`);
      return;
    }

    try {
      if (isEditing && budget) {
        const payload: UpdateBudgetRequest = {
          name: data.name,
          percentage: data.percentage,
          categoryIds: data.categoryIds,
          isRecurrent: data.isRecurrent,
        };

        await updateMutation.mutateAsync({ id: budget.id, data: payload });
      } else {
        const payload: CreateBudgetRequest = {
          name: data.name,
          percentage: data.percentage,
          referenceMonth,
          referenceYear,
          categoryIds: data.categoryIds,
          isRecurrent: data.isRecurrent,
        };

        await createMutation.mutateAsync(payload);
      }

      onSuccess();
    } catch (error) {
      setApiError(getErrorMessage(error));
    }
  }

  return (
    <form className="space-y-5" onSubmit={form.handleSubmit(handleSubmit)}>
      <div className="space-y-2">
        <label htmlFor="budget-name" className="text-sm font-medium text-slate-700">Nome</label>
        <Input id="budget-name" placeholder="Ex: Moradia" {...form.register('name')} />
        {form.formState.errors.name ? (
          <p className="text-xs text-red-600">{form.formState.errors.name.message}</p>
        ) : null}
      </div>

      <div className="space-y-2">
        <label htmlFor="budget-percentage" className="text-sm font-medium text-slate-700">Percentual da Renda</label>
        <div className="relative">
          <Input
            id="budget-percentage"
            type="number"
            min={0.01}
            max={100}
            step={0.01}
            className="pr-8"
            value={selectedPercentage || ''}
            onChange={(event) => {
              const parsed = Number(event.target.value);
              form.setValue('percentage', Number.isFinite(parsed) ? parsed : 0);
            }}
          />
          <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-sm text-slate-500">%</span>
        </div>
        <p className="text-xs text-slate-500">
          Disponível: {isLoadingAvailable ? 'carregando...' : `${availablePercentage.toFixed(2)}%`}
        </p>
        {monthlyIncome > 0 ? (
          <p className="text-xs text-slate-500">
            = {formatCurrency(projectedAmount)} ({formatCurrency(monthlyIncome)} × {(selectedPercentage || 0).toFixed(2)}%)
          </p>
        ) : null}
        {form.formState.errors.percentage || percentageError ? (
          <p className="text-xs text-red-600">{form.formState.errors.percentage?.message ?? percentageError}</p>
        ) : null}
      </div>

      <div className="space-y-2">
        <label className="text-sm font-medium text-slate-700">Mês de Referência</label>
        {isEditing && budget ? (
          <div className="rounded-md border bg-slate-50 px-3 py-2 text-sm text-slate-700">
            {formatCompetenceMonth(budget.referenceMonth, budget.referenceYear)}
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Select
              value={referenceMonth.toString()}
              onValueChange={(value) => {
                const parsedMonth = Number(value);
                setReferenceMonth(parsedMonth);
                form.setValue('referenceMonth', parsedMonth);
              }}
            >
              <SelectTrigger aria-label="Selecionar mês de referência">
                <SelectValue placeholder="Mês" />
              </SelectTrigger>
              <SelectContent>
                {MONTHS.map((monthOption) => (
                  <SelectItem key={monthOption.value} value={monthOption.value.toString()}>
                    {monthOption.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              value={referenceYear.toString()}
              onValueChange={(value) => {
                const parsedYear = Number(value);
                setReferenceYear(parsedYear);
                form.setValue('referenceYear', parsedYear);
              }}
            >
              <SelectTrigger aria-label="Selecionar ano de referência">
                <SelectValue placeholder="Ano" />
              </SelectTrigger>
              <SelectContent>
                {years.map((yearOption) => (
                  <SelectItem key={yearOption} value={yearOption.toString()}>
                    {yearOption}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}
      </div>

      <div className="space-y-2">
        <label className="text-sm font-medium text-slate-700">Categorias (Despesa)</label>
        <div className="max-h-52 space-y-2 overflow-y-auto rounded-md border p-3">
          {categories.map((category) => {
            const isSelected = selectedCategoryIds.includes(category.id);
            const usedByBudgetName = categoryBudgetMap.get(category.id);
            const isDisabled = Boolean(usedByBudgetName);
            const disabledTitle = usedByBudgetName
              ? `Em uso no orçamento ${usedByBudgetName}`
              : undefined;

            return (
              <button
                key={category.id}
                type="button"
                className={`w-full rounded-md border px-3 py-2 text-left text-sm transition-colors ${isSelected ? 'border-primary bg-primary/10 text-primary' : 'border-slate-200 bg-white text-slate-700'} ${isDisabled ? 'cursor-not-allowed opacity-50' : 'hover:border-primary/40 hover:bg-slate-50'}`}
                onClick={() => {
                  if (!isDisabled) {
                    toggleCategory(category.id);
                  }
                }}
                disabled={isDisabled}
                title={disabledTitle}
                aria-pressed={isSelected}
              >
                <div className="flex items-center justify-between gap-2">
                  <span>{category.name}</span>
                  {isDisabled ? <Badge variant="outline">Em uso</Badge> : null}
                </div>
              </button>
            );
          })}
        </div>
        {form.formState.errors.categoryIds ? (
          <p className="text-xs text-red-600">{form.formState.errors.categoryIds.message}</p>
        ) : null}
      </div>

      <div className="flex items-center justify-between rounded-md border p-3">
        <div className="space-y-0.5">
          <p className="text-sm font-medium text-slate-700">Recorrente</p>
          <p className="text-xs text-slate-500">Repetir mensalmente</p>
        </div>
        <Switch
          checked={form.watch('isRecurrent')}
          onCheckedChange={(checked) => form.setValue('isRecurrent', checked)}
          aria-label="Repetir mensalmente"
        />
      </div>

      {apiError ? <p className="text-sm text-red-600">{apiError}</p> : null}

      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
          {isEditing ? 'Salvar Alterações' : 'Criar Orçamento'}
        </Button>
      </div>
    </form>
  );
}
