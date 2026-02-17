namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class TransferOnlyBetweenNonCardAccountsException : DomainException
{
    public TransferOnlyBetweenNonCardAccountsException()
        : base("Transferências são permitidas apenas entre contas não cartão.")
    {
    }
}