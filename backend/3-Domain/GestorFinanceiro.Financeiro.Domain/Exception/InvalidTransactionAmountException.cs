namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InvalidTransactionAmountException : DomainException
{
    public InvalidTransactionAmountException(decimal amount)
        : base($"Transaction amount must be greater than zero. Provided: {amount}.")
    {
    }
}
