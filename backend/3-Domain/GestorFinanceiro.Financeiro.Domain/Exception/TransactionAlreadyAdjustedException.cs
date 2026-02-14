namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionAlreadyAdjustedException : DomainException
{
    public TransactionAlreadyAdjustedException(Guid transactionId)
        : base($"Transaction with ID '{transactionId}' has already been adjusted.")
    {
    }
}