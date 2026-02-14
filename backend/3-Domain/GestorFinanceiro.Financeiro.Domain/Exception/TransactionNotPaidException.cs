namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionNotPaidException : DomainException
{
    public TransactionNotPaidException(Guid transactionId)
        : base($"Transaction with ID '{transactionId}' is not paid and cannot be adjusted.")
    {
    }
}