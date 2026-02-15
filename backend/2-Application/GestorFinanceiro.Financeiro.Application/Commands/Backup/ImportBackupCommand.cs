using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;

namespace GestorFinanceiro.Financeiro.Application.Commands.Backup;

public record ImportBackupCommand(BackupDataDto Data) : ICommand<BackupImportSummaryDto>;
