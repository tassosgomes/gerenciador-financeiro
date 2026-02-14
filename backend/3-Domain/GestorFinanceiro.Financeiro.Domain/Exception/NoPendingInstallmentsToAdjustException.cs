namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class NoPendingInstallmentsToAdjustException : DomainException
{
    public NoPendingInstallmentsToAdjustException()
        : base("Nenhuma parcela pendente para ajustar.")
    {
    }
}
