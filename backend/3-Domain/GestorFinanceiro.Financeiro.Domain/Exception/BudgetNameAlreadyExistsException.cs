namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class BudgetNameAlreadyExistsException : DomainException
{
    public BudgetNameAlreadyExistsException(string name)
        : base($"Já existe um orçamento com o nome '{name}'.")
    {
    }
}
