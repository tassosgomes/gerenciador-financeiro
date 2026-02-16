using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.API.Extensions;
using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Account;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AccountsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccountResponse>> CreateAsync([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateAccountCommand(
            request.Name,
            request.Type!.Value,
            request.InitialBalance,
            request.AllowNegativeBalance,
            userId.ToString(),
            null, // OperationId
            request.CreditLimit,
            request.ClosingDay,
            request.DueDay,
            request.DebitAccountId,
            request.EnforceCreditLimit);

        var response = await _dispatcher.DispatchCommandAsync<CreateAccountCommand, AccountResponse>(command, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> ListAsync(
        [FromQuery] bool? isActive,
        [FromQuery] AccountType? type,
        CancellationToken cancellationToken)
    {
        var query = new ListAccountsQuery(isActive, type);
        var response = await _dispatcher.DispatchQueryAsync<ListAccountsQuery, IReadOnlyList<AccountResponse>>(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAccountByIdQuery(id);
        var response = await _dispatcher.DispatchQueryAsync<GetAccountByIdQuery, AccountResponse>(query, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> UpdateAsync(Guid id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdateAccountCommand(
            id,
            request.Name,
            request.AllowNegativeBalance!.Value,
            userId.ToString(),
            null, // OperationId
            request.CreditLimit,
            request.ClosingDay,
            request.DueDay,
            request.DebitAccountId,
            request.EnforceCreditLimit);
        var response = await _dispatcher.DispatchCommandAsync<UpdateAccountCommand, AccountResponse>(command, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatusAsync(Guid id, [FromBody] UpdateAccountStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (request.IsActive!.Value)
        {
            var activateCommand = new ActivateAccountCommand(id, userId.ToString());
            await _dispatcher.DispatchCommandAsync<ActivateAccountCommand, Unit>(activateCommand, cancellationToken);
        }
        else
        {
            var deactivateCommand = new DeactivateAccountCommand(id, userId.ToString());
            await _dispatcher.DispatchCommandAsync<DeactivateAccountCommand, Unit>(deactivateCommand, cancellationToken);
        }

        return NoContent();
    }
}
