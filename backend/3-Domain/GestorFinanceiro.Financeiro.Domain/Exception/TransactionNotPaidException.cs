namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionNotPaidException : DomainException
{
    public TransactionNotPaidException(Guid transactionId)
        : base("Apenas transações pagas podem ser ajustadas.")
    {
    }
}