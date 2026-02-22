namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record BudgetSummaryResponse(
    int ReferenceYear,
    int ReferenceMonth,
    decimal MonthlyIncome,
    decimal TotalBudgetedPercentage,
    decimal TotalBudgetedAmount,
    decimal TotalConsumedAmount,
    decimal TotalRemainingAmount,
    decimal UnbudgetedPercentage,
    decimal UnbudgetedAmount,
    decimal UnbudgetedExpenses,
    IReadOnlyList<BudgetResponse> Budgets
);
