using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class RecurrenceTemplateRepository : Repository<RecurrenceTemplate>, IRecurrenceTemplateRepository
{
    public RecurrenceTemplateRepository(FinanceiroDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<RecurrenceTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken)
    {
        return await _context.RecurrenceTemplates
            .AsNoTracking()
            .Where(template => template.IsActive)
            .OrderBy(template => template.DayOfMonth)
            .ToListAsync(cancellationToken);
    }
}
