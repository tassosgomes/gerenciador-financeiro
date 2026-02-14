namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InactiveUserException : DomainException
{
    public InactiveUserException(Guid userId)
        : base($"User '{userId}' is inactive and cannot authenticate.")
    {
    }
}
