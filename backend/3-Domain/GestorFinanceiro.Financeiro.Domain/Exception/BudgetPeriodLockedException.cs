namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class BudgetPeriodLockedException : DomainException
{
    public BudgetPeriodLockedException(Guid? budgetId, int month, int year)
        : base(CreateMessage(budgetId, month, year))
    {
    }

    private static string CreateMessage(Guid? budgetId, int month, int year)
    {
        if (budgetId.HasValue)
        {
            return $"Não é permitido alterar o orçamento '{budgetId}' de período encerrado ({month:D2}/{year}).";
        }

        return $"O período {month:D2}/{year} está encerrado. Apenas mês corrente ou futuro é permitido.";
    }
}
