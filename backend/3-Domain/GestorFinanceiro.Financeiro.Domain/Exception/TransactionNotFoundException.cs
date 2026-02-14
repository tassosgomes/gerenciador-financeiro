namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionNotFoundException : DomainException
{
    public TransactionNotFoundException(Guid transactionId)
        : base($"Transaction with ID '{transactionId}' not found.")
    {
    }
}