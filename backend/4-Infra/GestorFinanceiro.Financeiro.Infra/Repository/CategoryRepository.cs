using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.Infra.Model;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasLinkedDataAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var hasTransactions = await _context.Transactions
            .AsNoTracking()
            .AnyAsync(transaction => transaction.CategoryId == categoryId, cancellationToken);

        if (hasTransactions)
        {
            return true;
        }

        var hasRecurrenceTemplates = await _context.RecurrenceTemplates
            .AsNoTracking()
            .AnyAsync(template => template.CategoryId == categoryId, cancellationToken);

        if (hasRecurrenceTemplates)
        {
            return true;
        }

        return await _context.Set<BudgetCategoryLink>()
            .AsNoTracking()
            .AnyAsync(link => link.CategoryId == categoryId, cancellationToken);
    }

    public async Task MigrateLinkedDataAsync(
        Guid sourceCategoryId,
        Guid targetCategoryId,
        string userId,
        CancellationToken cancellationToken)
    {
        var sourceCategoryParam = new NpgsqlParameter("sourceCategoryId", sourceCategoryId);
        var targetCategoryParam = new NpgsqlParameter("targetCategoryId", targetCategoryId);
        var userIdParam = new NpgsqlParameter("userId", userId);

        const string sql = """
            UPDATE transactions
            SET category_id = @targetCategoryId,
                updated_by = @userId,
                updated_at = NOW()
            WHERE category_id = @sourceCategoryId;

            UPDATE recurrence_templates
            SET category_id = @targetCategoryId,
                updated_by = @userId,
                updated_at = NOW()
            WHERE category_id = @sourceCategoryId;

            DELETE FROM budget_categories
            WHERE category_id = @sourceCategoryId;
            """;

        await _context.Database.ExecuteSqlRawAsync(
            sql,
            [sourceCategoryParam, targetCategoryParam, userIdParam],
            cancellationToken);
    }

    public void Remove(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        _context.Categories.Remove(category);
    }
}
