namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class BudgetMustHaveCategoriesException : DomainException
{
    public BudgetMustHaveCategoriesException(Guid budgetId)
        : base($"O orçamento '{budgetId}' deve ter pelo menos uma categoria vinculada.")
    {
    }

    public BudgetMustHaveCategoriesException(string budgetName)
        : base($"O orçamento '{budgetName}' deve ter pelo menos uma categoria vinculada.")
    {
    }
}
