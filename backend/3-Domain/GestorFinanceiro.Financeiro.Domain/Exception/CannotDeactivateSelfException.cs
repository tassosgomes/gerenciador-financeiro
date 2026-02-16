namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class CannotDeactivateSelfException : DomainException
{
    public CannotDeactivateSelfException()
        : base("Cannot deactivate your own user account.")
    {
    }
}
