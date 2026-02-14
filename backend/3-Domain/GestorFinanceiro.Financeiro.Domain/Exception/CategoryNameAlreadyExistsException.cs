using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class CategoryNameAlreadyExistsException : DomainException
{
    public CategoryNameAlreadyExistsException(string name, CategoryType type)
        : base($"Category with name '{name}' and type '{type}' already exists.")
    {
    }
}