namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionNotPendingException : DomainException
{
    public TransactionNotPendingException(Guid transactionId)
        : base($"Transaction '{transactionId}' is not pending.")
    {
    }
}
