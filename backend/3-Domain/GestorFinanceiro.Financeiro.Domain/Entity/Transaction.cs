using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class Transaction : BaseEntity
{
    public Guid AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CompetenceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public TransactionStatus Status { get; private set; }
    public bool IsAdjustment { get; private set; }
    public Guid? OriginalTransactionId { get; private set; }
    public bool HasAdjustment { get; private set; }
    public Guid? InstallmentGroupId { get; private set; }
    public int? InstallmentNumber { get; private set; }
    public int? TotalInstallments { get; private set; }
    public bool IsRecurrent { get; private set; }
    public Guid? RecurrenceTemplateId { get; private set; }
    public Guid? TransferGroupId { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? OperationId { get; private set; }

    public bool IsOverdue =>
        Status == TransactionStatus.Pending
        && DueDate.HasValue
        && DueDate.Value.Date < DateTime.UtcNow.Date;

    public Account Account { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
    public Transaction? OriginalTransaction { get; private set; }

    public static Transaction Create(
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        string description,
        DateTime competenceDate,
        DateTime? dueDate,
        TransactionStatus status,
        string userId,
        string? operationId = null)
    {
        if (amount <= 0)
        {
            throw new InvalidTransactionAmountException(amount);
        }

        var transaction = new Transaction
        {
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Description = description,
            CompetenceDate = competenceDate,
            DueDate = dueDate,
            Status = status,
            OperationId = operationId,
        };

        transaction.SetAuditOnCreate(userId);
        return transaction;
    }

    public static Transaction Restore(
        Guid id,
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        string description,
        DateTime competenceDate,
        DateTime? dueDate,
        TransactionStatus status,
        bool isAdjustment,
        Guid? originalTransactionId,
        bool hasAdjustment,
        Guid? installmentGroupId,
        int? installmentNumber,
        int? totalInstallments,
        bool isRecurrent,
        Guid? recurrenceTemplateId,
        Guid? transferGroupId,
        string? cancellationReason,
        string? cancelledBy,
        DateTime? cancelledAt,
        string? operationId,
        string createdBy,
        DateTime createdAt,
        string? updatedBy,
        DateTime? updatedAt)
    {
        return new Transaction
        {
            Id = id,
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Description = description,
            CompetenceDate = competenceDate,
            DueDate = dueDate,
            Status = status,
            IsAdjustment = isAdjustment,
            OriginalTransactionId = originalTransactionId,
            HasAdjustment = hasAdjustment,
            InstallmentGroupId = installmentGroupId,
            InstallmentNumber = installmentNumber,
            TotalInstallments = totalInstallments,
            IsRecurrent = isRecurrent,
            RecurrenceTemplateId = recurrenceTemplateId,
            TransferGroupId = transferGroupId,
            CancellationReason = cancellationReason,
            CancelledBy = cancelledBy,
            CancelledAt = cancelledAt,
            OperationId = operationId,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedBy = updatedBy,
            UpdatedAt = updatedAt
        };
    }

    public static Transaction CreateAdjustment(
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal differenceAmount,
        Guid originalTransactionId,
        string description,
        DateTime competenceDate,
        string userId,
        string? operationId = null)
    {
        var adjustment = Create(
            accountId,
            categoryId,
            type,
            differenceAmount,
            description,
            competenceDate,
            null,
            TransactionStatus.Paid,
            userId,
            operationId);

        adjustment.IsAdjustment = true;
        adjustment.OriginalTransactionId = originalTransactionId;

        return adjustment;
    }

    public void Cancel(string userId, string? reason = null)
    {
        if (Status == TransactionStatus.Cancelled)
        {
            throw new TransactionAlreadyCancelledException(Id);
        }

        Status = TransactionStatus.Cancelled;
        CancellationReason = reason;
        CancelledBy = userId;
        CancelledAt = DateTime.UtcNow;
        SetAuditOnUpdate(userId);
    }

    public void MarkAsAdjusted(string userId)
    {
        HasAdjustment = true;
        SetAuditOnUpdate(userId);
    }

    public void SetInstallmentInfo(Guid groupId, int number, int total)
    {
        InstallmentGroupId = groupId;
        InstallmentNumber = number;
        TotalInstallments = total;
    }

    public void SetRecurrenceInfo(Guid templateId)
    {
        IsRecurrent = true;
        RecurrenceTemplateId = templateId;
    }

    public void SetTransferGroup(Guid transferGroupId)
    {
        TransferGroupId = transferGroupId;
    }
}
