namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId)
        : base($"User with ID '{userId}' not found.")
    {
    }
}
