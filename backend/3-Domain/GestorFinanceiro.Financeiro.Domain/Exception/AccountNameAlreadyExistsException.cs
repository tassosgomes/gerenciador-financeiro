namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class AccountNameAlreadyExistsException : DomainException
{
    public AccountNameAlreadyExistsException(string name)
        : base($"Account with name '{name}' already exists.")
    {
    }
}