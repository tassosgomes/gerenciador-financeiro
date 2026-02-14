namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionAlreadyCancelledException : DomainException
{
    public TransactionAlreadyCancelledException(Guid transactionId)
        : base($"Transaction '{transactionId}' is already cancelled.")
    {
    }
}
