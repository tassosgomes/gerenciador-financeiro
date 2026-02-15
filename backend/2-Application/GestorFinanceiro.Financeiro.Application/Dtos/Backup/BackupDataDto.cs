namespace GestorFinanceiro.Financeiro.Application.Dtos.Backup;

public record BackupDataDto(
    IReadOnlyList<UserBackupDto> Users,
    IReadOnlyList<AccountBackupDto> Accounts,
    IReadOnlyList<CategoryBackupDto> Categories,
    IReadOnlyList<TransactionBackupDto> Transactions,
    IReadOnlyList<RecurrenceTemplateBackupDto> RecurrenceTemplates);
