using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Invoice;

public record PayInvoiceCommand(
    Guid CreditCardAccountId,
    decimal Amount,
    DateTime CompetenceDate,
    string UserId,
    string? OperationId = null) : ICommand<IReadOnlyList<TransactionResponse>>;
