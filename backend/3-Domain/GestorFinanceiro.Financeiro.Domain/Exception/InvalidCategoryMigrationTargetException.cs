namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InvalidCategoryMigrationTargetException : DomainException
{
    public InvalidCategoryMigrationTargetException(string message)
        : base(message)
    {
    }
}
