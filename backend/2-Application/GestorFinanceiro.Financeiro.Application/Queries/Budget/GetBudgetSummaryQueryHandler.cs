using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Budget;

public class GetBudgetSummaryQueryHandler : IQueryHandler<GetBudgetSummaryQuery, BudgetSummaryResponse>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<GetBudgetSummaryQueryHandler> _logger;

    public GetBudgetSummaryQueryHandler(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository,
        ILogger<GetBudgetSummaryQueryHandler> logger)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<BudgetSummaryResponse> HandleAsync(
        GetBudgetSummaryQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting budget summary for {Month}/{Year}", query.Month, query.Year);

        var budgetsTask = _budgetRepository.GetByMonthAsync(query.Year, query.Month, cancellationToken);
        var monthlyIncomeTask = _budgetRepository.GetMonthlyIncomeAsync(query.Year, query.Month, cancellationToken);
        var unbudgetedExpensesTask = _budgetRepository.GetUnbudgetedExpensesAsync(query.Year, query.Month, cancellationToken);
        var allCategoriesTask = _categoryRepository.GetAllAsync(cancellationToken);

        await Task.WhenAll(budgetsTask, monthlyIncomeTask, unbudgetedExpensesTask, allCategoriesTask);

        var budgets = await budgetsTask;
        var monthlyIncome = await monthlyIncomeTask;
        var unbudgetedExpenses = await unbudgetedExpensesTask;
        var categoryNames = (await allCategoriesTask)
            .ToDictionary(category => category.Id, category => category.Name);

        var budgetResponses = await Task.WhenAll(
            budgets.Select(
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
                }));

        var totalBudgetedPercentage = budgets.Sum(budget => budget.Percentage);
        var totalBudgetedAmount = budgetResponses.Sum(budget => budget.LimitAmount);
        var totalConsumedAmount = budgetResponses.Sum(budget => budget.ConsumedAmount);
        var totalRemainingAmount = totalBudgetedAmount - totalConsumedAmount;
        var unbudgetedPercentage = 100m - totalBudgetedPercentage;
        var unbudgetedAmount = monthlyIncome * (unbudgetedPercentage / 100m);

        return new BudgetSummaryResponse(
            query.Year,
            query.Month,
            monthlyIncome,
            totalBudgetedPercentage,
            totalBudgetedAmount,
            totalConsumedAmount,
            totalRemainingAmount,
            unbudgetedPercentage,
            unbudgetedAmount,
            unbudgetedExpenses,
            budgetResponses);
    }
}
