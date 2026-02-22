using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.Infra.Model;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class BudgetRepository : Repository<Budget>, IBudgetRepository
{
    public BudgetRepository(FinanceiroDbContext context)
        : base(context)
    {
    }

    public override async Task<Budget> AddAsync(Budget entity, CancellationToken cancellationToken)
    {
        await base.AddAsync(entity, cancellationToken);
        await AddBudgetCategoriesAsync(entity, cancellationToken);
        return entity;
    }

    public override void Update(Budget entity)
    {
        base.Update(entity);

        var existingLinks = _context.Set<BudgetCategoryLink>()
            .Where(link => link.BudgetId == entity.Id)
            .ToList();

        _context.Set<BudgetCategoryLink>().RemoveRange(existingLinks);

        var categoryLinks = CreateCategoryLinks(entity);
        _context.Set<BudgetCategoryLink>().AddRange(categoryLinks);
    }

    public async Task<IReadOnlyList<Budget>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken)
    {
        var budgets = await _context.Budgets
            .AsNoTracking()
            .Where(budget => budget.ReferenceYear == year && budget.ReferenceMonth == month)
            .OrderBy(budget => budget.Name)
            .ToListAsync(cancellationToken);

        return await RestoreWithCategoriesAsync(budgets, cancellationToken);
    }

    public async Task<Budget?> GetByIdWithCategoriesAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await _context.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (budget is null)
        {
            return null;
        }

        var categoryIds = await _context.Set<BudgetCategoryLink>()
            .AsNoTracking()
            .Where(link => link.BudgetId == budget.Id)
            .OrderBy(link => link.CategoryId)
            .Select(link => link.CategoryId)
            .ToListAsync(cancellationToken);

        return RestoreBudget(budget, categoryIds);
    }

    public async Task<IReadOnlyList<Budget>> GetRecurrentBudgetsForMonthAsync(int year, int month, CancellationToken cancellationToken)
    {
        var budgets = await _context.Budgets
            .AsNoTracking()
            .Where(budget => budget.ReferenceYear == year
                             && budget.ReferenceMonth == month
                             && budget.IsRecurrent)
            .OrderBy(budget => budget.Name)
            .ToListAsync(cancellationToken);

        return await RestoreWithCategoriesAsync(budgets, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeBudgetId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await _context.Budgets
            .AsNoTracking()
            .AnyAsync(
                budget => budget.Name == name && (!excludeBudgetId.HasValue || budget.Id != excludeBudgetId.Value),
                cancellationToken);
    }

    public async Task<decimal> GetTotalPercentageForMonthAsync(int year, int month, Guid? excludeBudgetId, CancellationToken cancellationToken)
    {
        return await _context.Budgets
            .AsNoTracking()
            .Where(budget => budget.ReferenceYear == year
                             && budget.ReferenceMonth == month
                             && (!excludeBudgetId.HasValue || budget.Id != excludeBudgetId.Value))
            .SumAsync(budget => (decimal?)budget.Percentage ?? 0m, cancellationToken);
    }

    public async Task<bool> IsCategoryUsedInMonthAsync(
        Guid categoryId,
        int year,
        int month,
        Guid? excludeBudgetId,
        CancellationToken cancellationToken)
    {
        return await _context.Set<BudgetCategoryLink>()
            .AsNoTracking()
            .AnyAsync(
                link => link.CategoryId == categoryId
                        && link.ReferenceYear == year
                        && link.ReferenceMonth == month
                        && (!excludeBudgetId.HasValue || link.BudgetId != excludeBudgetId.Value),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetUsedCategoryIdsForMonthAsync(
        int year,
        int month,
        Guid? excludeBudgetId,
        CancellationToken cancellationToken)
    {
        return await _context.Set<BudgetCategoryLink>()
            .AsNoTracking()
            .Where(link => link.ReferenceYear == year
                           && link.ReferenceMonth == month
                           && (!excludeBudgetId.HasValue || link.BudgetId != excludeBudgetId.Value))
            .Select(link => link.CategoryId)
            .Distinct()
            .OrderBy(categoryId => categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetMonthlyIncomeAsync(int year, int month, CancellationToken cancellationToken)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return await _context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Type == TransactionType.Credit
                                  && transaction.Status == TransactionStatus.Paid
                                  && transaction.CompetenceDate >= startDate
                                  && transaction.CompetenceDate < endDate)
            .SumAsync(transaction => (decimal?)transaction.Amount ?? 0m, cancellationToken);
    }

    public async Task<decimal> GetConsumedAmountAsync(
        IReadOnlyList<Guid> categoryIds,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(categoryIds);

        if (categoryIds.Count == 0)
        {
            return 0m;
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return await _context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Type == TransactionType.Debit
                                  && transaction.Status == TransactionStatus.Paid
                                  && transaction.CompetenceDate >= startDate
                                  && transaction.CompetenceDate < endDate
                                  && categoryIds.Contains(transaction.CategoryId))
            .SumAsync(transaction => (decimal?)transaction.Amount ?? 0m, cancellationToken);
    }

    public async Task<decimal> GetUnbudgetedExpensesAsync(int year, int month, CancellationToken cancellationToken)
    {
        var budgetedCategoryIds = await _context.Set<BudgetCategoryLink>()
            .AsNoTracking()
            .Where(link => link.ReferenceYear == year && link.ReferenceMonth == month)
            .Select(link => link.CategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var query = _context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Type == TransactionType.Debit
                                  && transaction.Status == TransactionStatus.Paid
                                  && transaction.CompetenceDate >= startDate
                                  && transaction.CompetenceDate < endDate);

        if (budgetedCategoryIds.Count > 0)
        {
            query = query.Where(transaction => !budgetedCategoryIds.Contains(transaction.CategoryId));
        }

        return await query.SumAsync(transaction => (decimal?)transaction.Amount ?? 0m, cancellationToken);
    }

    public async Task RemoveCategoryFromBudgetsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM budget_categories WHERE category_id = {categoryId}",
            cancellationToken);
    }

    public void Remove(Budget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        _context.Budgets.Remove(budget);
    }

    private async Task AddBudgetCategoriesAsync(Budget budget, CancellationToken cancellationToken)
    {
        var categoryLinks = CreateCategoryLinks(budget);
        await _context.Set<BudgetCategoryLink>().AddRangeAsync(categoryLinks, cancellationToken);
    }

    private static IReadOnlyList<BudgetCategoryLink> CreateCategoryLinks(Budget budget)
    {
        var categoryIds = budget.CategoryIds.Distinct().ToList();
        return categoryIds
            .Select(categoryId => new BudgetCategoryLink
            {
                BudgetId = budget.Id,
                CategoryId = categoryId,
                ReferenceYear = (short)budget.ReferenceYear,
                ReferenceMonth = (short)budget.ReferenceMonth
            })
            .ToList();
    }

    private async Task<IReadOnlyList<Budget>> RestoreWithCategoriesAsync(
        IReadOnlyList<Budget> budgets,
        CancellationToken cancellationToken)
    {
        if (budgets.Count == 0)
        {
            return [];
        }

        var budgetIds = budgets.Select(budget => budget.Id).ToList();

        var categoriesByBudget = await _context.Set<BudgetCategoryLink>()
            .AsNoTracking()
            .Where(link => budgetIds.Contains(link.BudgetId))
            .GroupBy(link => link.BudgetId)
            .Select(group => new
            {
                BudgetId = group.Key,
                CategoryIds = group
                    .OrderBy(link => link.CategoryId)
                    .Select(link => link.CategoryId)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var categoryMap = categoriesByBudget.ToDictionary(item => item.BudgetId, item => (IReadOnlyList<Guid>)item.CategoryIds);

        return budgets
            .Select(budget =>
            {
                var categoryIds = categoryMap.GetValueOrDefault(budget.Id, []);
                return RestoreBudget(budget, categoryIds);
            })
            .ToList();
    }

    private static Budget RestoreBudget(Budget source, IReadOnlyList<Guid> categoryIds)
    {
        return Budget.Restore(
            source.Id,
            source.Name,
            source.Percentage,
            source.ReferenceYear,
            source.ReferenceMonth,
            categoryIds,
            source.IsRecurrent,
            source.CreatedBy,
            source.CreatedAt,
            source.UpdatedBy,
            source.UpdatedAt);
    }
}