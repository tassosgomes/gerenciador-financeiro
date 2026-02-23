using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.API.Extensions;
using GestorFinanceiro.Financeiro.Application.Commands.Receipt;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Receipt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/receipts")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ReceiptsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("lookup")]
    [ProducesResponseType<NfceLookupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NfceLookupResponse>> LookupAsync([FromBody] LookupNfceRequest request, CancellationToken cancellationToken)
    {
        var query = new LookupNfceQuery(request.Input);
        var response = await _dispatcher.DispatchQueryAsync<LookupNfceQuery, NfceLookupResponse>(query, cancellationToken);
        return Ok(response);
    }

    [HttpPost("import")]
    [ProducesResponseType<ImportNfceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ImportNfceResponse>> ImportAsync([FromBody] ImportNfceRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new ImportNfceCommand(
            request.AccessKey,
            request.AccountId,
            request.CategoryId,
            request.Description,
            request.CompetenceDate,
            userId.ToString(),
            request.OperationId);

        var response = await _dispatcher.DispatchCommandAsync<ImportNfceCommand, ImportNfceResponse>(command, cancellationToken);
        return Created($"/api/v1/transactions/{response.Transaction.Id}", response);
    }
}
