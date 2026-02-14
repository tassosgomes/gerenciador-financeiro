namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InstallmentPaidCannotBeCancelledException : DomainException
{
    public InstallmentPaidCannotBeCancelledException(Guid transactionId)
        : base($"Installment transaction '{transactionId}' is paid and cannot be cancelled.")
    {
    }
}
