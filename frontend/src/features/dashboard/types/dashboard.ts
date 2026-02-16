export interface DashboardSummaryResponse {
  totalBalance: number;
  monthlyIncome: number;
  monthlyExpenses: number;
  creditCardDebt: number;
  totalCreditLimit: number | null;
  creditUtilizationPercent: number | null;
}

export interface MonthlyComparisonDto {
  month: string;
  income: number;
  expenses: number;
}

export interface CategoryExpenseDto {
  categoryId: string;
  categoryName: string;
  total: number;
  percentage: number;
}

export interface DashboardChartsResponse {
  revenueVsExpense: MonthlyComparisonDto[];
  expenseByCategory: CategoryExpenseDto[];
}
