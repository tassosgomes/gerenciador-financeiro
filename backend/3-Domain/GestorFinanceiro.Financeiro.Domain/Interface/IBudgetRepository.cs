using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IBudgetRepository : IRepository<Budget>
{
    Task<IReadOnlyList<Budget>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken);
    Task<IReadOnlyList<Budget>> GetBudgetsByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<Budget?> GetByIdWithCategoriesAsync(Guid id, CancellationToken cancellationToken);
    Task<int> GetCategoryCountAsync(Guid budgetId, CancellationToken cancellationToken);
    Task<decimal> GetTotalPercentageForMonthAsync(int year, int month, Guid? excludeBudgetId, CancellationToken cancellationToken);
    Task<bool> IsCategoryUsedInMonthAsync(Guid categoryId, int year, int month, Guid? excludeBudgetId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetUsedCategoryIdsForMonthAsync(int year, int month, Guid? excludeBudgetId, CancellationToken cancellationToken);
    Task<decimal> GetMonthlyIncomeAsync(int year, int month, CancellationToken cancellationToken);
    Task<decimal> GetConsumedAmountAsync(IReadOnlyList<Guid> categoryIds, int year, int month, CancellationToken cancellationToken);
    Task<decimal> GetUnbudgetedExpensesAsync(int year, int month, CancellationToken cancellationToken);
    Task<IReadOnlyList<Budget>> GetRecurrentBudgetsForMonthAsync(int year, int month, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeBudgetId, CancellationToken cancellationToken);
    Task RemoveCategoryFromBudgetsAsync(Guid categoryId, CancellationToken cancellationToken);
    void Remove(Budget budget);
}
