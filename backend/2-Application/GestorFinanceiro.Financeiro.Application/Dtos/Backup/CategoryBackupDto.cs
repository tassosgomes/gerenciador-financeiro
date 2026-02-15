using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Dtos.Backup;

public record CategoryBackupDto(
    Guid Id,
    string Name,
    CategoryType Type,
    bool IsActive,
    bool IsSystem,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt);
