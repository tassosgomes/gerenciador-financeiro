namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class AdjustmentAmountUnchangedException : DomainException
{
    public AdjustmentAmountUnchangedException()
        : base("Valor correto é igual ao original.")
    {
    }
}
