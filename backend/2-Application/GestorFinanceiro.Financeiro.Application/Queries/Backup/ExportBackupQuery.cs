using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;

namespace GestorFinanceiro.Financeiro.Application.Queries.Backup;

public record ExportBackupQuery : IQuery<BackupExportDto>;
