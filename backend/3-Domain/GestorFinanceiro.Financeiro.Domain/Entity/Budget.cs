using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class Budget : BaseEntity
{
    private readonly List<Guid> _categoryIds = [];

    protected Budget()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public decimal Percentage { get; private set; }
    public int ReferenceYear { get; private set; }
    public int ReferenceMonth { get; private set; }
    public bool IsRecurrent { get; private set; }
    public IReadOnlyList<Guid> CategoryIds => _categoryIds.AsReadOnly();

    public static Budget Create(
        string name,
        decimal percentage,
        int referenceYear,
        int referenceMonth,
        IReadOnlyList<Guid> categoryIds,
        bool isRecurrent,
        string userId)
    {
        ValidateName(name);
        ValidatePercentage(percentage);
        ValidateReferenceMonth(referenceMonth);
        ValidateReferenceYear(referenceYear);
        ValidateCategories(categoryIds, name);

        var budget = new Budget
        {
            Name = name,
            Percentage = percentage,
            ReferenceYear = referenceYear,
            ReferenceMonth = referenceMonth,
            IsRecurrent = isRecurrent
        };

        budget._categoryIds.AddRange(categoryIds);
        budget.SetAuditOnCreate(userId);

        return budget;
    }

    public static Budget Restore(
        Guid id,
        string name,
        decimal percentage,
        int referenceYear,
        int referenceMonth,
        IReadOnlyList<Guid> categoryIds,
        bool isRecurrent,
        string createdBy,
        DateTime createdAt,
        string? updatedBy,
        DateTime? updatedAt)
    {
        ValidateName(name);
        ValidatePercentage(percentage);
        ValidateReferenceMonth(referenceMonth);
        ValidateReferenceYear(referenceYear);
        ValidateCategories(categoryIds, name);

        var budget = new Budget
        {
            Id = id,
            Name = name,
            Percentage = percentage,
            ReferenceYear = referenceYear,
            ReferenceMonth = referenceMonth,
            IsRecurrent = isRecurrent,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedBy = updatedBy,
            UpdatedAt = updatedAt
        };

        budget._categoryIds.AddRange(categoryIds);

        return budget;
    }

    public void Update(
        string name,
        decimal percentage,
        IReadOnlyList<Guid> categoryIds,
        bool isRecurrent,
        string userId)
    {
        ValidateName(name);
        ValidatePercentage(percentage);
        ValidateCategories(categoryIds, name);

        Name = name;
        Percentage = percentage;
        IsRecurrent = isRecurrent;

        _categoryIds.Clear();
        _categoryIds.AddRange(categoryIds);

        SetAuditOnUpdate(userId);
    }

    public decimal CalculateLimit(decimal monthlyIncome)
    {
        return monthlyIncome * (Percentage / 100m);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome do orçamento é obrigatório.", nameof(name));
        }

        if (name.Length < 2 || name.Length > 150)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "O nome do orçamento deve ter entre 2 e 150 caracteres.");
        }
    }

    private static void ValidatePercentage(decimal percentage)
    {
        if (percentage <= 0 || percentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "O percentual do orçamento deve ser maior que 0 e menor ou igual a 100.");
        }
    }

    private static void ValidateReferenceMonth(int referenceMonth)
    {
        if (referenceMonth < 1 || referenceMonth > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceMonth), "O mês de referência deve estar entre 1 e 12.");
        }
    }

    private static void ValidateReferenceYear(int referenceYear)
    {
        if (referenceYear <= 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceYear), "O ano de referência deve ser maior que 2000.");
        }
    }

    private static void ValidateCategories(IReadOnlyList<Guid> categoryIds, string budgetName)
    {
        if (categoryIds.Count == 0)
        {
            throw new BudgetMustHaveCategoriesException(budgetName);
        }
    }
}
