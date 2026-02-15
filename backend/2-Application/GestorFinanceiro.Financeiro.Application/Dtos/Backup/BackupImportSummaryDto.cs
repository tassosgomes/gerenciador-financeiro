namespace GestorFinanceiro.Financeiro.Application.Dtos.Backup;

public record BackupImportSummaryDto(
    int Users,
    int Accounts,
    int Categories,
    int RecurrenceTemplates,
    int Transactions);
