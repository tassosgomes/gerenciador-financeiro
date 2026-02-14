namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class RecurrenceTemplateNotFoundException : DomainException
{
    public RecurrenceTemplateNotFoundException(Guid id)
        : base($"Recurrence template with ID '{id}' not found.")
    {
    }
}