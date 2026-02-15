using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class BackupRepository : IBackupRepository
{
    private readonly FinanceiroDbContext _context;

    public BackupRepository(FinanceiroDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .AsNoTracking()
            .OrderBy(account => account.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .AsNoTracking()
            .OrderBy(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecurrenceTemplate>> GetRecurrenceTemplatesAsync(CancellationToken cancellationToken)
    {
        return await _context.RecurrenceTemplates
            .AsNoTracking()
            .OrderBy(template => template.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task TruncateAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            TRUNCATE TABLE
                audit_logs,
                refresh_tokens,
                transactions,
                recurrence_templates,
                categories,
                accounts,
                users
            RESTART IDENTITY;
            """;

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task ImportAsync(
        IReadOnlyCollection<User> users,
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<Category> categories,
        IReadOnlyCollection<RecurrenceTemplate> recurrenceTemplates,
        IReadOnlyCollection<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        await _context.Users.AddRangeAsync(users, cancellationToken);
        await _context.Accounts.AddRangeAsync(accounts, cancellationToken);
        await _context.Categories.AddRangeAsync(categories, cancellationToken);
        await _context.RecurrenceTemplates.AddRangeAsync(recurrenceTemplates, cancellationToken);
        await _context.Transactions.AddRangeAsync(transactions, cancellationToken);
    }
}
