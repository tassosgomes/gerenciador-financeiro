namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid accountId)
        : base($"Account with ID '{accountId}' not found.")
    {
    }
}