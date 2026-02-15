using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IBackupRepository
{
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RecurrenceTemplate>> GetRecurrenceTemplatesAsync(CancellationToken cancellationToken);
    Task TruncateAllAsync(CancellationToken cancellationToken);
    Task ImportAsync(
        IReadOnlyCollection<User> users,
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<Category> categories,
        IReadOnlyCollection<RecurrenceTemplate> recurrenceTemplates,
        IReadOnlyCollection<Transaction> transactions,
        CancellationToken cancellationToken);
}
