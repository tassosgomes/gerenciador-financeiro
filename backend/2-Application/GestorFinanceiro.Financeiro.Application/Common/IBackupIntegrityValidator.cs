using GestorFinanceiro.Financeiro.Application.Dtos.Backup;

namespace GestorFinanceiro.Financeiro.Application.Common;

public interface IBackupIntegrityValidator
{
    IReadOnlyList<string> Validate(BackupDataDto data);
}
