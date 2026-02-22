using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;

namespace GestorFinanceiro.Financeiro.Domain.Service;

public class BudgetDomainService
{
    public async Task ValidatePercentageCapAsync(
        IBudgetRepository repository,
        int year,
        int month,
        decimal newPercentage,
        Guid? excludeBudgetId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var currentTotal = await repository.GetTotalPercentageForMonthAsync(
            year,
            month,
            excludeBudgetId,
            cancellationToken);

        var resultingTotal = currentTotal + newPercentage;
        if (resultingTotal > 100m)
        {
            var available = Math.Max(0m, 100m - currentTotal);
            throw new BudgetPercentageExceededException(newPercentage, available, month, year);
        }
    }

    public async Task ValidateCategoryUniquenessAsync(
        IBudgetRepository repository,
        IReadOnlyList<Guid> categoryIds,
        int year,
        int month,
        Guid? excludeBudgetId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(categoryIds);

        foreach (var categoryId in categoryIds.Distinct())
        {
            var inUse = await repository.IsCategoryUsedInMonthAsync(
                categoryId,
                year,
                month,
                excludeBudgetId,
                cancellationToken);

            if (inUse)
            {
                throw new CategoryAlreadyBudgetedException(categoryId, "outro orçamento", month, year);
            }
        }
    }

    public void ValidateReferenceMonth(int year, int month)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "O mês de referência deve estar entre 1 e 12.");
        }

        if (year <= 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "O ano de referência deve ser maior que 2000.");
        }

        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var requestedMonth = new DateTime(year, month, 1);

        if (requestedMonth < currentMonth)
        {
            throw new BudgetPeriodLockedException(null, month, year);
        }
    }
}
