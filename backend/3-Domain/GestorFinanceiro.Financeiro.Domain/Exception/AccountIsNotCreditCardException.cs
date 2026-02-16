namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class AccountIsNotCreditCardException : DomainException
{
    public AccountIsNotCreditCardException(Guid accountId)
        : base($"Account '{accountId}' is not a credit card.")
    {
    }
}
