namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class UserEmailAlreadyExistsException : DomainException
{
    public UserEmailAlreadyExistsException(string email)
        : base($"A user with email '{email}' already exists.")
    {
    }
}
