using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class RecurrenceTemplate : BaseEntity
{
    public Guid AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int DayOfMonth { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastGeneratedDate { get; private set; }
    public TransactionStatus DefaultStatus { get; private set; }

    public static RecurrenceTemplate Create(
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        string description,
        int dayOfMonth,
        TransactionStatus defaultStatus,
        string userId)
    {
        var template = new RecurrenceTemplate
        {
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Description = description,
            DayOfMonth = dayOfMonth,
            DefaultStatus = defaultStatus,
        };

        template.SetAuditOnCreate(userId);
        return template;
    }

    public void Deactivate(string userId)
    {
        IsActive = false;
        SetAuditOnUpdate(userId);
    }

    public void MarkGenerated(DateTime generatedDate, string userId)
    {
        LastGeneratedDate = generatedDate;
        SetAuditOnUpdate(userId);
    }

    public bool ShouldGenerateForMonth(DateTime referenceDate)
    {
        if (!IsActive)
        {
            return false;
        }

        if (LastGeneratedDate is null)
        {
            return true;
        }

        return referenceDate.Year > LastGeneratedDate.Value.Year
            || (referenceDate.Year == LastGeneratedDate.Value.Year
                && referenceDate.Month > LastGeneratedDate.Value.Month);
    }
}
