namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class SystemCategoryCannotBeChangedException : DomainException
{
    public SystemCategoryCannotBeChangedException(Guid categoryId)
        : base($"Cannot modify system category with ID '{categoryId}'")
    {
    }
}
