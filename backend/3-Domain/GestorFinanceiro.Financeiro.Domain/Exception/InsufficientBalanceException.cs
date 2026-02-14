namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException(Guid accountId, decimal currentBalance, decimal debitAmount)
        : base($"Account '{accountId}' has insufficient balance. Current: {currentBalance}, debit: {debitAmount}.")
    {
    }
}
