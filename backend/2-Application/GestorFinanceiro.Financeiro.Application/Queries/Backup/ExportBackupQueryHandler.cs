using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;

namespace GestorFinanceiro.Financeiro.Application.Queries.Backup;

public class ExportBackupQueryHandler : IQueryHandler<ExportBackupQuery, BackupExportDto>
{
    private readonly IBackupRepository _backupRepository;

    public ExportBackupQueryHandler(IBackupRepository backupRepository)
    {
        _backupRepository = backupRepository;
    }

    public async Task<BackupExportDto> HandleAsync(ExportBackupQuery query, CancellationToken cancellationToken)
    {
        var users = await _backupRepository.GetUsersAsync(cancellationToken);
        var accounts = await _backupRepository.GetAccountsAsync(cancellationToken);
        var categories = await _backupRepository.GetCategoriesAsync(cancellationToken);
        var transactions = await _backupRepository.GetTransactionsAsync(cancellationToken);
        var recurrenceTemplates = await _backupRepository.GetRecurrenceTemplatesAsync(cancellationToken);

        var data = new BackupDataDto(
            users.Adapt<IReadOnlyList<UserBackupDto>>(),
            accounts.Adapt<IReadOnlyList<AccountBackupDto>>(),
            categories.Adapt<IReadOnlyList<CategoryBackupDto>>(),
            transactions.Adapt<IReadOnlyList<TransactionBackupDto>>(),
            recurrenceTemplates.Adapt<IReadOnlyList<RecurrenceTemplateBackupDto>>());

        return new BackupExportDto(DateTime.UtcNow, "1.0", data);
    }
}
