using GestorFinanceiro.Financeiro.Domain.Dto;

namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record DashboardChartsResponse(
    List<MonthlyComparisonDto> RevenueVsExpense,
    List<CategoryExpenseDto> ExpenseByCategory
);
