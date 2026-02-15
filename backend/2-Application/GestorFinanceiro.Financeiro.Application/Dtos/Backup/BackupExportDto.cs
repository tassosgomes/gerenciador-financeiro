namespace GestorFinanceiro.Financeiro.Application.Dtos.Backup;

public record BackupExportDto(
    DateTime ExportedAt,
    string Version,
    BackupDataDto Data);
