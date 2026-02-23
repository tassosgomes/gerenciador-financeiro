using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.API.Extensions;
using GestorFinanceiro.Financeiro.Application.Commands.Budget;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/budgets")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public BudgetsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BudgetResponse>> CreateAsync([FromBody] CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateBudgetCommand(
            request.Name,
            request.Percentage,
            request.ReferenceYear,
            request.ReferenceMonth,
            request.CategoryIds,
            request.IsRecurrent,
            userId.ToString());

        var response = await _dispatcher.DispatchCommandAsync<CreateBudgetCommand, BudgetResponse>(command, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BudgetResponse>> UpdateAsync(Guid id, [FromBody] UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdateBudgetCommand(
            id,
            request.Name,
            request.Percentage,
            request.CategoryIds,
            request.IsRecurrent,
            userId.ToString());

        var response = await _dispatcher.DispatchCommandAsync<UpdateBudgetCommand, BudgetResponse>(command, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new DeleteBudgetCommand(id, userId.ToString());
        await _dispatcher.DispatchCommandAsync<DeleteBudgetCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    [HttpGet("summary")]
    [ProducesResponseType<BudgetSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BudgetSummaryResponse>> GetSummaryAsync([FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var query = new GetBudgetSummaryQuery(year, month);
        var response = await _dispatcher.DispatchQueryAsync<GetBudgetSummaryQuery, BudgetSummaryResponse>(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("available-percentage")]
    [ProducesResponseType<AvailablePercentageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AvailablePercentageResponse>> GetAvailablePercentageAsync(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] Guid? excludeBudgetId,
        CancellationToken cancellationToken)
    {
        var query = new GetAvailablePercentageQuery(year, month, excludeBudgetId);
        var response = await _dispatcher.DispatchQueryAsync<GetAvailablePercentageQuery, AvailablePercentageResponse>(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BudgetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BudgetResponse>>> ListAsync([FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var query = new ListBudgetsQuery(year, month);
        var response = await _dispatcher.DispatchQueryAsync<ListBudgetsQuery, IReadOnlyList<BudgetResponse>>(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetBudgetByIdQuery(id);
        var response = await _dispatcher.DispatchQueryAsync<GetBudgetByIdQuery, BudgetResponse>(query, cancellationToken);

        return Ok(response);
    }
}
