export interface BudgetCategoryDto {
  id: string;
  name: string;
}

export interface BudgetResponse {
  id: string;
  name: string;
  percentage: number;
  referenceYear: number;
  referenceMonth: number;
  isRecurrent: boolean;
  monthlyIncome: number;
  limitAmount: number;
  consumedAmount: number;
  remainingAmount: number;
  consumedPercentage: number;
  categories: BudgetCategoryDto[];
  createdAt: string;
  updatedAt: string | null;
}

export interface BudgetSummaryResponse {
  referenceYear: number;
  referenceMonth: number;
  monthlyIncome: number;
  totalBudgetedPercentage: number;
  totalBudgetedAmount: number;
  totalConsumedAmount: number;
  totalRemainingAmount: number;
  unbudgetedPercentage: number;
  unbudgetedAmount: number;
  unbudgetedExpenses: number;
  budgets: BudgetResponse[];
}

export interface AvailablePercentageResponse {
  usedPercentage: number;
  availablePercentage: number;
  usedCategoryIds: string[];
}

export interface CreateBudgetRequest {
  name: string;
  percentage: number;
  referenceYear: number;
  referenceMonth: number;
  categoryIds: string[];
  isRecurrent: boolean;
}

export interface UpdateBudgetRequest {
  name: string;
  percentage: number;
  categoryIds: string[];
  isRecurrent: boolean;
}