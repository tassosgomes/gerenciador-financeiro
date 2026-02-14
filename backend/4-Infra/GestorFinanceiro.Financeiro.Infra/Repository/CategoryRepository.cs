using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(FinanceiroDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsByNameAndTypeAsync(string name, CategoryType type, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await _context.Categories
            .AsNoTracking()
            .AnyAsync(category => category.Name == name && category.Type == type, cancellationToken);
    }
}
