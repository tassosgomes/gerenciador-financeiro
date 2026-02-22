namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class BudgetNotFoundException : DomainException
{
    public BudgetNotFoundException(Guid budgetId)
        : base($"Orçamento com ID '{budgetId}' não foi encontrado.")
    {
    }
}
