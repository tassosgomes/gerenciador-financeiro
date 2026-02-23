import { http, HttpResponse } from 'msw';

import type {
  AvailablePercentageResponse,
  BudgetResponse,
  BudgetSummaryResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from '@/features/budgets/types';

const categoryIds = {
  supermarket: '11111111-1111-4111-8111-111111111111',
  restaurant: '22222222-2222-4222-8222-222222222222',
  transport: '33333333-3333-4333-8333-333333333333',
  leisure: '44444444-4444-4444-8444-444444444444',
  housing: '55555555-5555-4555-8555-555555555555',
  subscriptions: '66666666-6666-4666-8666-666666666666',
  health: '77777777-7777-4777-8777-777777777777',
  travel: '88888888-8888-4888-8888-888888888888',
} as const;

export const lowConsumptionBudget: BudgetResponse = {
  id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
  name: 'Alimentação Essencial',
  percentage: 25,
  referenceYear: 2026,
  referenceMonth: 2,
  isRecurrent: false,
  monthlyIncome: 10000,
  limitAmount: 2500,
  consumedAmount: 750,
  remainingAmount: 1750,
  consumedPercentage: 30,
  categories: [
    { id: categoryIds.supermarket, name: 'Supermercado' },
    { id: categoryIds.restaurant, name: 'Restaurante' },
  ],
  createdAt: '2026-02-01T00:00:00Z',
  updatedAt: null,
};

export const mediumHighConsumptionBudget: BudgetResponse = {
  id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
  name: 'Transporte e Mobilidade',
  percentage: 12,
  referenceYear: 2026,
  referenceMonth: 2,
  isRecurrent: true,
  monthlyIncome: 10000,
  limitAmount: 1200,
  consumedAmount: 900,
  remainingAmount: 300,
  consumedPercentage: 75,
  categories: [{ id: categoryIds.transport, name: 'Transporte' }],
  createdAt: '2026-02-01T00:00:00Z',
  updatedAt: null,
};

export const highConsumptionBudget: BudgetResponse = {
  id: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
  name: 'Moradia',
  percentage: 20,
  referenceYear: 2026,
  referenceMonth: 2,
  isRecurrent: true,
  monthlyIncome: 10000,
  limitAmount: 2000,
  consumedAmount: 1840,
  remainingAmount: 160,
  consumedPercentage: 92,
  categories: [{ id: categoryIds.housing, name: 'Moradia' }],
  createdAt: '2026-02-01T00:00:00Z',
  updatedAt: null,
};

export const exceededBudget: BudgetResponse = {
  id: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
  name: 'Lazer',
  percentage: 10,
  referenceYear: 2026,
  referenceMonth: 2,
  isRecurrent: false,
  monthlyIncome: 10000,
  limitAmount: 1000,
  consumedAmount: 1150,
  remainingAmount: -150,
  consumedPercentage: 115,
  categories: [{ id: categoryIds.leisure, name: 'Lazer' }],
  createdAt: '2026-02-01T00:00:00Z',
  updatedAt: null,
};

export const alertBudget: BudgetResponse = {
  id: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
  name: 'Serviços e Assinaturas',
  percentage: 8,
  referenceYear: 2026,
  referenceMonth: 2,
  isRecurrent: true,
  monthlyIncome: 10000,
  limitAmount: 800,
  consumedAmount: 656,
  remainingAmount: 144,
  consumedPercentage: 82,
  categories: [{ id: categoryIds.subscriptions, name: 'Assinaturas' }],
  createdAt: '2026-02-01T00:00:00Z',
  updatedAt: null,
};

const januaryBudget: BudgetResponse = {
  id: 'ffffffff-ffff-4fff-8fff-ffffffffffff',
  name: 'Saúde',
  percentage: 15,
  referenceYear: 2026,
  referenceMonth: 1,
  isRecurrent: false,
  monthlyIncome: 9000,
  limitAmount: 1350,
  consumedAmount: 420,
  remainingAmount: 930,
  consumedPercentage: 31.1,
  categories: [{ id: categoryIds.health, name: 'Saúde' }],
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
};

const marchBudget: BudgetResponse = {
  id: '99999999-9999-4999-8999-999999999999',
  name: 'Planejamento Março',
  percentage: 18,
  referenceYear: 2026,
  referenceMonth: 3,
  isRecurrent: false,
  monthlyIncome: 12000,
  limitAmount: 2160,
  consumedAmount: 540,
  remainingAmount: 1620,
  consumedPercentage: 25,
  categories: [{ id: categoryIds.travel, name: 'Viagem' }],
  createdAt: '2026-03-01T00:00:00Z',
  updatedAt: null,
};

const budgetStore = new Map<string, BudgetResponse[]>([
  ['2026-1', [januaryBudget]],
  ['2026-2', [
    lowConsumptionBudget,
    mediumHighConsumptionBudget,
    highConsumptionBudget,
    exceededBudget,
    alertBudget,
  ]],
  ['2026-3', [marchBudget]],
]);

function getKey(year: number, month: number): string {
  return `${year}-${month}`;
}

function getBudgetsForMonth(year: number, month: number): BudgetResponse[] {
  return [...(budgetStore.get(getKey(year, month)) ?? [])];
}

function buildSummary(year: number, month: number): BudgetSummaryResponse {
  const budgets = getBudgetsForMonth(year, month);

  if (year === 2026 && month === 2) {
    return {
      referenceYear: 2026,
      referenceMonth: 2,
      monthlyIncome: 10000,
      totalBudgetedPercentage: 75,
      totalBudgetedAmount: 7500,
      totalConsumedAmount: 5296,
      totalRemainingAmount: 2204,
      unbudgetedPercentage: 25,
      unbudgetedAmount: 2500,
      unbudgetedExpenses: 420,
      budgets,
    };
  }

  if (year === 2026 && month === 3) {
    return {
      referenceYear: 2026,
      referenceMonth: 3,
      monthlyIncome: 12000,
      totalBudgetedPercentage: 18,
      totalBudgetedAmount: 2160,
      totalConsumedAmount: 540,
      totalRemainingAmount: 1620,
      unbudgetedPercentage: 82,
      unbudgetedAmount: 9840,
      unbudgetedExpenses: 100,
      budgets,
    };
  }

  return {
    referenceYear: year,
    referenceMonth: month,
    monthlyIncome: 9000,
    totalBudgetedPercentage: 15,
    totalBudgetedAmount: 1350,
    totalConsumedAmount: 420,
    totalRemainingAmount: 930,
    unbudgetedPercentage: 85,
    unbudgetedAmount: 7650,
    unbudgetedExpenses: 80,
    budgets,
  };
}

function buildAvailablePercentage(year: number, month: number, excludeBudgetId?: string): AvailablePercentageResponse {
  const budgets = getBudgetsForMonth(year, month);
  const usedPercentage = budgets.reduce((sum, budget) => sum + budget.percentage, 0);
  const excludedPercentage = excludeBudgetId
    ? budgets.find((budget) => budget.id === excludeBudgetId)?.percentage ?? 0
    : 0;
  const availablePercentage = 100 - (usedPercentage - excludedPercentage);

  return {
    usedPercentage,
    availablePercentage,
    usedCategoryIds: budgets.flatMap((budget) => budget.categories.map((category) => category.id)),
  };
}

export const budgetsHandlers = [
  http.get('*/api/v1/budgets/summary', ({ request }) => {
    const url = new URL(request.url);
    const year = Number(url.searchParams.get('year') ?? 2026);
    const month = Number(url.searchParams.get('month') ?? 2);

    return HttpResponse.json(buildSummary(year, month));
  }),

  http.get('*/api/v1/budgets', ({ request }) => {
    const url = new URL(request.url);
    const year = Number(url.searchParams.get('year') ?? 2026);
    const month = Number(url.searchParams.get('month') ?? 2);

    return HttpResponse.json(getBudgetsForMonth(year, month));
  }),

  http.get('*/api/v1/budgets/available-percentage', ({ request }) => {
    const url = new URL(request.url);
    const year = Number(url.searchParams.get('year') ?? 2026);
    const month = Number(url.searchParams.get('month') ?? 2);
    const excludeBudgetId = url.searchParams.get('excludeBudgetId') ?? undefined;

    return HttpResponse.json(buildAvailablePercentage(year, month, excludeBudgetId));
  }),

  http.post('*/api/v1/budgets', async ({ request }) => {
    const body = (await request.json()) as CreateBudgetRequest;
    const key = getKey(body.referenceYear, body.referenceMonth);
    const budgets = budgetStore.get(key) ?? [];
    const categories = body.categoryIds.map((categoryId) => ({
      id: categoryId,
      name: `Categoria ${categoryId.slice(0, 4)}`,
    }));
    const monthlyIncome = body.referenceYear === 2026 && body.referenceMonth === 3 ? 12000 : 10000;
    const limitAmount = monthlyIncome * (body.percentage / 100);

    const createdBudget: BudgetResponse = {
      id: 'abababab-abab-4bab-8bab-abababababab',
      name: body.name,
      percentage: body.percentage,
      referenceYear: body.referenceYear,
      referenceMonth: body.referenceMonth,
      isRecurrent: body.isRecurrent,
      monthlyIncome,
      limitAmount,
      consumedAmount: 0,
      remainingAmount: limitAmount,
      consumedPercentage: 0,
      categories,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };

    budgetStore.set(key, [...budgets, createdBudget]);

    return HttpResponse.json(createdBudget, { status: 201 });
  }),

  http.put('*/api/v1/budgets/:id', async ({ params, request }) => {
    const body = (await request.json()) as UpdateBudgetRequest;
    const id = String(params.id);

    for (const [key, budgets] of budgetStore.entries()) {
      const targetIndex = budgets.findIndex((budget) => budget.id === id);

      if (targetIndex >= 0) {
        const target = budgets[targetIndex];
        const updatedBudget: BudgetResponse = {
          ...target,
          name: body.name,
          percentage: body.percentage,
          isRecurrent: body.isRecurrent,
          limitAmount: target.monthlyIncome * (body.percentage / 100),
          categories: body.categoryIds.map((categoryId) => ({
            id: categoryId,
            name: `Categoria ${categoryId.slice(0, 4)}`,
          })),
          updatedAt: new Date().toISOString(),
        };

        const updatedBudgets = [...budgets];
        updatedBudgets[targetIndex] = updatedBudget;
        budgetStore.set(key, updatedBudgets);

        return HttpResponse.json(updatedBudget);
      }
    }

    return new HttpResponse(null, { status: 404 });
  }),

  http.delete('*/api/v1/budgets/:id', ({ params }) => {
    const id = String(params.id);

    for (const [key, budgets] of budgetStore.entries()) {
      const updatedBudgets = budgets.filter((budget) => budget.id !== id);

      if (updatedBudgets.length !== budgets.length) {
        budgetStore.set(key, updatedBudgets);
        return new HttpResponse(null, { status: 204 });
      }
    }

    return new HttpResponse(null, { status: 404 });
  }),
];
