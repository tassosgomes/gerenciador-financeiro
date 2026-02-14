using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public CategoryType Type { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public void UpdateName(string newName, string userId)
    {
        Name = newName;
        SetAuditOnUpdate(userId);
    }
}
