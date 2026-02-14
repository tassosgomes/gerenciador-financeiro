using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IRecurrenceTemplateRepository : IRepository<RecurrenceTemplate>
{
    Task<IEnumerable<RecurrenceTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken);
}
