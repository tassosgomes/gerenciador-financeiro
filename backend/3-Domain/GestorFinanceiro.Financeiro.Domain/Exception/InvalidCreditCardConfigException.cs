namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InvalidCreditCardConfigException : DomainException
{
    public InvalidCreditCardConfigException(string message) 
        : base(message)
    {
    }
}
