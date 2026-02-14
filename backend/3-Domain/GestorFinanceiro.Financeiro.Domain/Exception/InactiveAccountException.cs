namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InactiveAccountException : DomainException
{
    public InactiveAccountException(Guid accountId)
        : base($"Account '{accountId}' is inactive and cannot receive transactions.")
    {
    }
}
