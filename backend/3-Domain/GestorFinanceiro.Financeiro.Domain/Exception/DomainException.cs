namespace GestorFinanceiro.Financeiro.Domain.Exception;

public abstract class DomainException : System.Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}
