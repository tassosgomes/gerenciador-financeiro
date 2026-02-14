namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class CategoryNotFoundException : DomainException
{
    public CategoryNotFoundException(Guid categoryId)
        : base($"Category with ID '{categoryId}' not found.")
    {
    }
}