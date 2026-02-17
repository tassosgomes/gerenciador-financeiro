using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class SystemRepository : ISystemRepository
{
    private readonly FinanceiroDbContext _context;

    public SystemRepository(FinanceiroDbContext context)
    {
        _context = context;
    }

    public async Task ResetSystemDataAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            -- Delete all transactions (respects FK constraints)
            DELETE FROM transactions;
            
            -- Delete all recurrence templates
            DELETE FROM recurrence_templates;

            -- Delete credit card details before accounts (debit_account_id FK)
            DELETE FROM credit_card_details;
            
            -- Delete all accounts
            DELETE FROM accounts;
            
            -- Delete only user-created categories (keep system categories)
            DELETE FROM categories WHERE is_system = false;
            """;

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
