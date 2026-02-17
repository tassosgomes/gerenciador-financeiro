namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class CategoryMigrationRequiredException : DomainException
{
    public CategoryMigrationRequiredException(Guid categoryId)
        : base($"Category '{categoryId}' has linked records. Choose a target category to migrate before deletion.")
    {
    }
}
