using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.API.Extensions;
using GestorFinanceiro.Financeiro.Application.Commands.Invoice;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/accounts/{accountId:guid}/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public InvoicesController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoiceAsync(
        [FromRoute] Guid accountId,
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var query = new GetInvoiceQuery(accountId, month, year);
        var response = await _dispatcher.DispatchQueryAsync<GetInvoiceQuery, InvoiceResponse>(query, cancellationToken);
        return Ok(response);
    }

    [HttpPost("pay")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> PayInvoiceAsync(
        [FromRoute] Guid accountId,
        [FromBody] PayInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new PayInvoiceCommand(
            accountId,
            request.Amount,
            request.CompetenceDate!.Value,
            userId.ToString(),
            request.OperationId);

        var response = await _dispatcher.DispatchCommandAsync<PayInvoiceCommand, IReadOnlyList<TransactionResponse>>(command, cancellationToken);
        return Ok(response);
    }
}
