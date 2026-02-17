namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransactionNotFoundException : DomainException
{
    public TransactionNotFoundException(Guid transactionId)
        : base("Transação não encontrada.")
    {
    }
}