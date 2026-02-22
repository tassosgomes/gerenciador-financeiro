namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InvalidBudgetCategoryTypeException : DomainException
{
    public InvalidBudgetCategoryTypeException(Guid categoryId)
        : base($"A categoria '{categoryId}' não é do tipo Despesa e não pode ser usada em orçamentos.")
    {
    }
}
