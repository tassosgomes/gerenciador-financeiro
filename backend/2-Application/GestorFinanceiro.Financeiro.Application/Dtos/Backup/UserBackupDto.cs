using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Dtos.Backup;

public record UserBackupDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    bool MustChangePassword,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt);
