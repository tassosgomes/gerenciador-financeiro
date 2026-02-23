using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record TransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    string Description,
    DateTime CompetenceDate,
    DateTime? DueDate,
    TransactionStatus Status,
    bool IsAdjustment,
    Guid? OriginalTransactionId,
    bool HasAdjustment,
    Guid? InstallmentGroupId,
    int? InstallmentNumber,
    int? TotalInstallments,
    bool IsRecurrent,
    Guid? RecurrenceTemplateId,
    Guid? TransferGroupId,
    string? CancellationReason,
    string? CancelledBy,
    DateTime? CancelledAt,
    bool IsOverdue,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool HasReceipt = false
);