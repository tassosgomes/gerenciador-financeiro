using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface ICategoryRepository : IRepository<Category>
{
    Task<bool> ExistsByNameAndTypeAsync(string name, CategoryType type, CancellationToken cancellationToken);
}
