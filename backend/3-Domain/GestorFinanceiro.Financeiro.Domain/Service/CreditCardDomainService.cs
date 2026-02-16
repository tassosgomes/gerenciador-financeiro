using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Service;

public class CreditCardDomainService
{
    /// <summary>
    /// Calcula o período de fatura com base no dia de fechamento.
    /// Start: dia seguinte ao fechamento do mês anterior.
    /// End: dia de fechamento do mês do parâmetro.
    /// </summary>
    public (DateTime start, DateTime end) CalculateInvoicePeriod(
        int closingDay,
        int month,
        int year)
    {
        var end = new DateTime(year, month, closingDay);
        var previousMonth = end.AddMonths(-1);
        var start = previousMonth.AddDays(1);

        return (start, end);
    }

    /// <summary>
    /// Calcula o total da fatura: soma de débitos (positivo) - soma de créditos (negativo).
    /// </summary>
    public decimal CalculateInvoiceTotal(IEnumerable<Transaction> transactions)
    {
        return transactions.Sum(t =>
            t.Type == TransactionType.Debit ? t.Amount : -t.Amount);
    }
}
