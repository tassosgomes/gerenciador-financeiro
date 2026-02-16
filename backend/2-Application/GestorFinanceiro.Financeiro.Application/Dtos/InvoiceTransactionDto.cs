using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record InvoiceTransactionDto(
    Guid Id,
    string Description,
    decimal Amount,
    TransactionType Type,
    DateTime CompetenceDate,
    int? InstallmentNumber,
    int? TotalInstallments
);
