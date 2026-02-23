namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InvalidAccessKeyException : DomainException
{
    public InvalidAccessKeyException(string accessKey)
        : base($"Access key '{accessKey}' is invalid. Expected 44 numeric digits.")
    {
    }
}
