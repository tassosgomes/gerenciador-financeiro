using GestorFinanceiro.Financeiro.Domain.Dto;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class DashboardRepository : IDashboardRepository
{
    private readonly FinanceiroDbContext _context;

    public DashboardRepository(FinanceiroDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetTotalBalanceAsync(CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .Where(a => a.IsActive)
            .SumAsync(a => (decimal?)a.Balance ?? 0, cancellationToken);
    }

    public async Task<decimal> GetMonthlyIncomeAsync(int month, int year, CancellationToken cancellationToken)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Type == TransactionType.Credit
                     && t.Status == TransactionStatus.Paid
                     && t.CompetenceDate >= startDate
                     && t.CompetenceDate < endDate)
            .SumAsync(t => (decimal?)t.Amount ?? 0, cancellationToken);
    }

    public async Task<decimal> GetMonthlyExpensesAsync(int month, int year, CancellationToken cancellationToken)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Type == TransactionType.Debit
                     && t.Status == TransactionStatus.Paid
                     && t.CompetenceDate >= startDate
                     && t.CompetenceDate < endDate)
            .SumAsync(t => (decimal?)t.Amount ?? 0, cancellationToken);
    }

    public async Task<decimal> GetCreditCardDebtAsync(CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Type == AccountType.Cartao && a.Balance < 0)
            .SumAsync(a => (decimal?)a.Balance ?? 0, cancellationToken);
    }

    public async Task<decimal?> GetTotalCreditLimitAsync(CancellationToken cancellationToken)
    {
        var hasActiveCreditCards = await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.Type == AccountType.Cartao && a.IsActive && a.CreditCard != null, cancellationToken);

        if (!hasActiveCreditCards)
        {
            return null;
        }

        return await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Type == AccountType.Cartao && a.IsActive && a.CreditCard != null)
            .SumAsync(a => (decimal?)a.CreditCard!.CreditLimit ?? 0, cancellationToken);
    }

    public async Task<List<MonthlyComparisonDto>> GetRevenueVsExpenseAsync(
        int month,
        int year,
        CancellationToken cancellationToken)
    {
        var endDate = new DateTime(year, month, 1);
        var startDate = endDate.AddMonths(-5);

        var monthlyData = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Status == TransactionStatus.Paid
                     && t.CompetenceDate >= startDate
                     && t.CompetenceDate < endDate.AddMonths(1))
            .GroupBy(t => new
            {
                Year = t.CompetenceDate.Year,
                Month = t.CompetenceDate.Month,
                t.Type
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Type,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var result = new List<MonthlyComparisonDto>();
        for (int i = 0; i < 6; i++)
        {
            var targetDate = startDate.AddMonths(i);
            var monthKey = $"{targetDate:yyyy-MM}";

            var income = monthlyData
                .Where(d => d.Year == targetDate.Year
                         && d.Month == targetDate.Month
                         && d.Type == TransactionType.Credit)
                .Sum(d => d.Total);

            var expenses = monthlyData
                .Where(d => d.Year == targetDate.Year
                         && d.Month == targetDate.Month
                         && d.Type == TransactionType.Debit)
                .Sum(d => d.Total);

            result.Add(new MonthlyComparisonDto(monthKey, income, expenses));
        }

        return result;
    }

    public async Task<List<CategoryExpenseDto>> GetExpenseByCategoryAsync(
        int month,
        int year,
        CancellationToken cancellationToken)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var categoryExpenses = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Type == TransactionType.Debit
                     && (t.Status == TransactionStatus.Paid || t.Status == TransactionStatus.Pending)
                     && t.CompetenceDate >= startDate
                     && t.CompetenceDate < endDate)
            .GroupBy(t => new
            {
                t.CategoryId,
                t.Category.Name
            })
            .Select(g => new
            {
                g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var totalExpenses = categoryExpenses.Sum(c => c.Total);

        return categoryExpenses
            .Select(c => new CategoryExpenseDto(
                c.CategoryId,
                c.CategoryName,
                c.Total,
                totalExpenses > 0 ? (c.Total / totalExpenses) * 100 : 0))
            .OrderByDescending(c => c.Total)
            .ToList();
    }
}
