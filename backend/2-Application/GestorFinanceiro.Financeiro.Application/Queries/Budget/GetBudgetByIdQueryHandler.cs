using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Budget;

public class GetBudgetByIdQueryHandler : IQueryHandler<GetBudgetByIdQuery, BudgetResponse>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<GetBudgetByIdQueryHandler> _logger;

    public GetBudgetByIdQueryHandler(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository,
        ILogger<GetBudgetByIdQueryHandler> logger)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<BudgetResponse> HandleAsync(
        GetBudgetByIdQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting budget details for {BudgetId}", query.Id);

        var budget = await _budgetRepository.GetByIdWithCategoriesAsync(query.Id, cancellationToken);
        if (budget is null)
        {
            throw new BudgetNotFoundException(query.Id);
        }

        var monthlyIncome = await _budgetRepository.GetMonthlyIncomeAsync(
            budget.ReferenceYear,
            budget.ReferenceMonth,
            cancellationToken);

        var consumedAmount = await _budgetRepository.GetConsumedAmountAsync(
            budget.CategoryIds,
            budget.ReferenceYear,
            budget.ReferenceMonth,
            cancellationToken);

        var allCategories = await _categoryRepository.GetAllAsync(cancellationToken);

        var categoryNames = allCategories
            .ToDictionary(category => category.Id, category => category.Name);

        var categories = budget.CategoryIds
            .Where(categoryNames.ContainsKey)
            .Select(categoryId => new BudgetCategoryDto(categoryId, categoryNames[categoryId]))
            .OrderBy(category => category.Name)
            .ToList();

        return BudgetResponseFactory.Build(
            budget,
            monthlyIncome,
            consumedAmount,
            categories);
    }
}
