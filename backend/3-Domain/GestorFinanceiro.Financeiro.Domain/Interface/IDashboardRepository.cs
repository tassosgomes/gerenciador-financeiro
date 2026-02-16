using GestorFinanceiro.Financeiro.Domain.Dto;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IDashboardRepository
{
    Task<decimal> GetTotalBalanceAsync(CancellationToken cancellationToken);
    Task<decimal> GetMonthlyIncomeAsync(int month, int year, CancellationToken cancellationToken);
    Task<decimal> GetMonthlyExpensesAsync(int month, int year, CancellationToken cancellationToken);
    Task<decimal> GetCreditCardDebtAsync(CancellationToken cancellationToken);
    Task<decimal?> GetTotalCreditLimitAsync(CancellationToken cancellationToken);
    Task<List<MonthlyComparisonDto>> GetRevenueVsExpenseAsync(int month, int year, CancellationToken cancellationToken);
    Task<List<CategoryExpenseDto>> GetExpenseByCategoryAsync(int month, int year, CancellationToken cancellationToken);
}
