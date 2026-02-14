using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Service;

public class RecurrenceDomainService
{
    private readonly TransactionDomainService _transactionService;

    public RecurrenceDomainService(TransactionDomainService transactionService)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
    }

    public Transaction? GenerateNextOccurrence(
        RecurrenceTemplate template,
        Account account,
        DateTime referenceDate,
        string userId)
    {
        if (!template.ShouldGenerateForMonth(referenceDate))
        {
            return null;
        }

        var competenceDate = new DateTime(
            referenceDate.Year,
            referenceDate.Month,
            Math.Min(template.DayOfMonth, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month)));

        var transaction = _transactionService.CreateTransaction(
            account,
            template.CategoryId,
            template.Type,
            template.Amount,
            template.Description,
            competenceDate,
            competenceDate,
            template.DefaultStatus,
            userId);

        transaction.SetRecurrenceInfo(template.Id);
        template.MarkGenerated(competenceDate, userId);

        return transaction;
    }
}
