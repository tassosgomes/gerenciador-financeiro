using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Budget;

public class ListBudgetsQueryHandler : IQueryHandler<ListBudgetsQuery, IReadOnlyList<BudgetResponse>>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<ListBudgetsQueryHandler> _logger;

    public ListBudgetsQueryHandler(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository,
        ILogger<ListBudgetsQueryHandler> logger)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BudgetResponse>> HandleAsync(
        ListBudgetsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing budgets for {Month}/{Year}", query.Month, query.Year);

        var budgets = await _budgetRepository.GetByMonthAsync(query.Year, query.Month, cancellationToken);
        if (budgets.Count == 0)
        {
            return [];
        }

        var monthlyIncomeTask = _budgetRepository.GetMonthlyIncomeAsync(query.Year, query.Month, cancellationToken);
        var allCategoriesTask = _categoryRepository.GetAllAsync(cancellationToken);

        await Task.WhenAll(monthlyIncomeTask, allCategoriesTask);

        var monthlyIncome = await monthlyIncomeTask;
        var categoryNames = (await allCategoriesTask)
            .ToDictionary(category => category.Id, category => category.Name);

        var responseTasks = budgets.Select(
            async budget =>
            {
                var consumedAmount = await _budgetRepository.GetConsumedAmountAsync(
                    budget.CategoryIds,
                    budget.ReferenceYear,
                    budget.ReferenceMonth,
                    cancellationToken);

                var categories = budget.CategoryIds
                    .Where(categoryNames.ContainsKey)
                    .Select(categoryId => new BudgetCategoryDto(categoryId, categoryNames[categoryId]))
                    .OrderBy(category => category.Name)
                    .ToList();

                return BudgetResponseFactory.Build(budget, monthlyIncome, consumedAmount, categories);
            });

        return await Task.WhenAll(responseTasks);
    }
}
