namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class CreditLimitExceededException : DomainException
{
    public CreditLimitExceededException(Guid accountId, decimal availableLimit, decimal requestedAmount)
        : base($"Limite de crédito excedido na conta {accountId}. Disponível: {availableLimit:C}, Solicitado: {requestedAmount:C}")
    {
    }
}
