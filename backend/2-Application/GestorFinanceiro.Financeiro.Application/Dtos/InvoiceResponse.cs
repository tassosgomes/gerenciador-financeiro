namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record InvoiceResponse(
    Guid AccountId,
    string AccountName,
    int Month,
    int Year,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime DueDate,
    decimal TotalAmount,
    decimal PreviousBalance,
    decimal AmountDue,
    IReadOnlyList<InvoiceTransactionDto> Transactions
);
