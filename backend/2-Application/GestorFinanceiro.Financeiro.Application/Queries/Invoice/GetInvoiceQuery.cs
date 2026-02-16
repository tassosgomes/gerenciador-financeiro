using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Invoice;

public record GetInvoiceQuery(
    Guid AccountId,
    int Month,
    int Year
) : IQuery<InvoiceResponse>;
