using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public CategoryType Type { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsSystem { get; private set; } = false;

    public static Category Create(string name, CategoryType type, string userId)
    {
        var category = new Category
        {
            Name = name,
            Type = type,
        };

        category.SetAuditOnCreate(userId);
        return category;
    }

    public static Category Restore(
        Guid id,
        string name,
        CategoryType type,
        bool isActive,
        bool isSystem,
        string createdBy,
        DateTime createdAt,
        string? updatedBy,
        DateTime? updatedAt)
    {
        return new Category
        {
            Id = id,
            Name = name,
            Type = type,
            IsActive = isActive,
            IsSystem = isSystem,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedBy = updatedBy,
            UpdatedAt = updatedAt
        };
    }

    public void UpdateName(string newName, string userId)
    {
        if (IsSystem)
        {
            throw new SystemCategoryCannotBeChangedException(Id);
        }

        Name = newName;
        SetAuditOnUpdate(userId);
    }
}
