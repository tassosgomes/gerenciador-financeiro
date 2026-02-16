using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(FinanceiroDbContext context)
        : base(context)
    {
    }

    public async Task<Account?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .FromSqlInterpolated($"SELECT * FROM accounts WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await _context.Accounts
            .AsNoTracking()
            .AnyAsync(account => account.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetActiveByTypeAsync(AccountType type, CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Where(account => account.Type == type && account.IsActive)
            .OrderBy(account => account.Name)
            .ToListAsync(cancellationToken);
    }
}
