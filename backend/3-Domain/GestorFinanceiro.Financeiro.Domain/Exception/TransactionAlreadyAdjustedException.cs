namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionAlreadyAdjustedException : DomainException
{
    public TransactionAlreadyAdjustedException(Guid transactionId)
        : base("Esta transação já foi ajustada.")
    {
    }
}