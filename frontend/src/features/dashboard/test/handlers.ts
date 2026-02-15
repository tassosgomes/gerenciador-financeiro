import { http, HttpResponse } from 'msw';

import type {
  DashboardSummaryResponse,
  DashboardChartsResponse,
} from '@/features/dashboard/types/dashboard';

export const dashboardHandlers = [
  http.get('/api/v1/dashboard/summary', () => {
    const response: DashboardSummaryResponse = {
      totalBalance: 15420.50,
      monthlyIncome: 8200.00,
      monthlyExpenses: 4100.00,
      creditCardDebt: 1450.00,
    };
    return HttpResponse.json(response);
  }),

  http.get('/api/v1/dashboard/charts', () => {
    const response: DashboardChartsResponse = {
      revenueVsExpense: [
        { month: '2025-09', income: 7500, expenses: 4200 },
        { month: '2025-10', income: 7800, expenses: 4300 },
        { month: '2025-11', income: 8000, expenses: 3900 },
        { month: '2025-12', income: 8100, expenses: 4500 },
        { month: '2026-01', income: 7900, expenses: 4000 },
        { month: '2026-02', income: 8200, expenses: 4100 },
      ],
      expenseByCategory: [
        {
          categoryId: '1',
          categoryName: 'Alimentação',
          total: 1500.00,
          percentage: 36.59,
        },
        {
          categoryId: '2',
          categoryName: 'Transporte',
          total: 800.00,
          percentage: 19.51,
        },
        {
          categoryId: '3',
          categoryName: 'Moradia',
          total: 1200.00,
          percentage: 29.27,
        },
        {
          categoryId: '4',
          categoryName: 'Lazer',
          total: 600.00,
          percentage: 14.63,
        },
      ],
    };
    return HttpResponse.json(response);
  }),
];
