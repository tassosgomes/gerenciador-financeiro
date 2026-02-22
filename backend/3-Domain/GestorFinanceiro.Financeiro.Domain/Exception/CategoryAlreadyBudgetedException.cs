namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class CategoryAlreadyBudgetedException : DomainException
{
    public CategoryAlreadyBudgetedException(Guid categoryId, string budgetName, int month, int year)
        : base($"A categoria '{categoryId}' já está vinculada ao orçamento '{budgetName}' no período {month:D2}/{year}.")
    {
    }
}
