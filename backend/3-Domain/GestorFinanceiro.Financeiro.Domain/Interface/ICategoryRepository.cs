using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> ExistsByNameAndTypeAsync(string name, CategoryType type, CancellationToken cancellationToken);
    Task<bool> HasLinkedDataAsync(Guid categoryId, CancellationToken cancellationToken);
    Task MigrateLinkedDataAsync(Guid sourceCategoryId, Guid targetCategoryId, string userId, CancellationToken cancellationToken);
    void Remove(Category category);
}
