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

        var monthlyIncome = await _budgetRepository.GetMonthlyIncomeAsync(query.Year, query.Month, cancellationToken);
        var allCategories = await _categoryRepository.GetAllAsync(cancellationToken);

        var categoryNames = allCategories
            .ToDictionary(category => category.Id, category => category.Name);

        var responses = new List<BudgetResponse>(budgets.Count);
        foreach (var budget in budgets)
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

            responses.Add(BudgetResponseFactory.Build(budget, monthlyIncome, consumedAmount, categories));
        }

        return responses;
    }
}
