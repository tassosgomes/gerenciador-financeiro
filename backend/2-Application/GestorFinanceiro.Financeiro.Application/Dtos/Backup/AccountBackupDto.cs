using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Dtos.Backup;

public record AccountBackupDto(
    Guid Id,
    string Name,
    AccountType Type,
    decimal Balance,
    bool AllowNegativeBalance,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt);
