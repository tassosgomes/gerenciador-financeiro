namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionAlreadyCancelledException : DomainException
{
    public TransactionAlreadyCancelledException(Guid transactionId)
        : base("Esta transação já foi cancelada.")
    {
    }
}
