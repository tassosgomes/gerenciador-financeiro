using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Infra.StartupTasks;

public sealed class BudgetRecurrenceWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BudgetRecurrenceWorker> _logger;

    public BudgetRecurrenceWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BudgetRecurrenceWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ProcessRecurrenceAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro ao processar recorrência de orçamentos");
        }
    }

    private async Task ProcessRecurrenceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var budgetRepository = scope.ServiceProvider.GetRequiredService<IBudgetRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var currentYear = now.Year;
        var currentMonth = now.Month;

        var previousMonth = currentMonth - 1;
        var previousYear = currentYear;

        if (previousMonth < 1)
        {
            previousMonth = 12;
            previousYear--;
        }

        var recurrentBudgets = await budgetRepository.GetRecurrentBudgetsForMonthAsync(previousYear, previousMonth, cancellationToken);

        foreach (var recurrentBudget in recurrentBudgets)
        {
            try
            {
                var budgetAlreadyExists = await budgetRepository.ExistsByNameAsync(
                    recurrentBudget.Name,
                    null,
                    cancellationToken);

                if (budgetAlreadyExists)
                {
                    _logger.LogInformation(
                        "Orçamento '{BudgetName}' já existe para {Month}/{Year}",
                        recurrentBudget.Name,
                        currentMonth,
                        currentYear);
                    continue;
                }

                var activeCategoryIds = await GetActiveCategoryIdsAsync(recurrentBudget, categoryRepository, cancellationToken);
                if (activeCategoryIds.Count == 0)
                {
                    _logger.LogWarning(
                        "Orçamento recorrente '{BudgetName}' ignorado: todas as categorias estão inativas",
                        recurrentBudget.Name);
                    continue;
                }

                var budget = Budget.Create(
                    recurrentBudget.Name,
                    recurrentBudget.Percentage,
                    currentYear,
                    currentMonth,
                    activeCategoryIds,
                    isRecurrent: true,
                    userId: "system");

                await budgetRepository.AddAsync(budget, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Orçamento recorrente '{BudgetName}' criado para {Month}/{Year}",
                    recurrentBudget.Name,
                    currentMonth,
                    currentYear);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Erro ao replicar orçamento recorrente '{BudgetName}' para {Month}/{Year}",
                    recurrentBudget.Name,
                    currentMonth,
                    currentYear);
            }
        }

        var totalPercentage = await budgetRepository.GetTotalPercentageForMonthAsync(
            currentYear,
            currentMonth,
            null,
            cancellationToken);

        if (totalPercentage > 100m)
        {
            _logger.LogWarning(
                "Soma de percentuais para {Month}/{Year} excede 100%: {TotalPercentage}%",
                currentMonth,
                currentYear,
                totalPercentage);
        }
    }

    private static async Task<IReadOnlyList<Guid>> GetActiveCategoryIdsAsync(
        Budget budget,
        ICategoryRepository categoryRepository,
        CancellationToken cancellationToken)
    {
        var activeCategoryIds = new List<Guid>();

        foreach (var categoryId in budget.CategoryIds.Distinct())
        {
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is not null && category.IsActive)
            {
                activeCategoryIds.Add(categoryId);
            }
        }

        return activeCategoryIds;
    }
}