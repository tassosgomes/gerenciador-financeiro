using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task<IReadOnlyList<Account>> GetActiveByTypeAsync(AccountType type, CancellationToken cancellationToken);
}
