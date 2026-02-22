using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Budget;

public class GetAvailablePercentageQueryHandler : IQueryHandler<GetAvailablePercentageQuery, AvailablePercentageResponse>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ILogger<GetAvailablePercentageQueryHandler> _logger;

    public GetAvailablePercentageQueryHandler(
        IBudgetRepository budgetRepository,
        ILogger<GetAvailablePercentageQueryHandler> logger)
    {
        _budgetRepository = budgetRepository;
        _logger = logger;
    }

    public async Task<AvailablePercentageResponse> HandleAsync(
        GetAvailablePercentageQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting available percentage for {Month}/{Year} with excluded budget {BudgetId}",
            query.Month,
            query.Year,
            query.ExcludeBudgetId);

        var usedPercentageTask = _budgetRepository.GetTotalPercentageForMonthAsync(
            query.Year,
            query.Month,
            query.ExcludeBudgetId,
            cancellationToken);

        var usedCategoryIdsTask = _budgetRepository.GetUsedCategoryIdsForMonthAsync(
            query.Year,
            query.Month,
            query.ExcludeBudgetId,
            cancellationToken);

        await Task.WhenAll(usedPercentageTask, usedCategoryIdsTask);

        var usedPercentage = await usedPercentageTask;
        var availablePercentage = 100m - usedPercentage;

        return new AvailablePercentageResponse(
            usedPercentage,
            availablePercentage,
            await usedCategoryIdsTask);
    }
}
