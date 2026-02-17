namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionNotPendingException : DomainException
{
    public TransactionNotPendingException(Guid transactionId)
        : base("Apenas transações pendentes podem ser marcadas como pagas.")
    {
    }
}
