using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.API.Extensions;
using GestorFinanceiro.Financeiro.Application.Commands.Installment;
using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;
using GestorFinanceiro.Financeiro.Application.Commands.Transaction;
using GestorFinanceiro.Financeiro.Application.Commands.Transfer;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Transaction;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public TransactionsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TransactionResponse>> CreateAsync([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateTransactionCommand(
            request.AccountId,
            request.CategoryId,
            request.Type!.Value,
            request.Amount,
            request.Description,
            request.CompetenceDate,
            request.DueDate,
            TransactionStatus.Pending,
            userId.ToString(),
            request.OperationId);

        var response = await _dispatcher.DispatchCommandAsync<CreateTransactionCommand, TransactionResponse>(command, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, response);
    }

    [HttpPost("installments")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> CreateInstallmentsAsync([FromBody] CreateInstallmentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateInstallmentCommand(
            request.AccountId,
            request.CategoryId,
            request.Type!.Value,
            request.Amount,
            request.NumberOfInstallments,
            request.Description,
            request.CompetenceDate,
            request.DueDate,
            userId.ToString(),
            request.OperationId);

        var response = await _dispatcher.DispatchCommandAsync<CreateInstallmentCommand, IReadOnlyList<TransactionResponse>>(command, cancellationToken);
        return Created("/api/v1/transactions/installments", response);
    }

    [HttpPost("recurrences")]
    [ProducesResponseType<RecurrenceTemplateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RecurrenceTemplateResponse>> CreateRecurrenceAsync([FromBody] CreateRecurrenceRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateRecurrenceCommand(
            request.AccountId,
            request.CategoryId,
            request.Type!.Value,
            request.Amount,
            request.Description,
            request.DayOfMonth ?? request.StartDate.Day,
            request.DefaultStatus ?? TransactionStatus.Pending,
            userId.ToString(),
            request.OperationId);

        var response = await _dispatcher.DispatchCommandAsync<CreateRecurrenceCommand, RecurrenceTemplateResponse>(command, cancellationToken);
        return Created($"/api/v1/transactions/recurrences/{response.Id}", response);
    }

    [HttpPost("transfers")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> CreateTransferAsync([FromBody] CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateTransferCommand(
            request.SourceAccountId,
            request.DestinationAccountId,
            request.CategoryId,
            request.Amount,
            request.Description,
            request.CompetenceDate,
            userId.ToString(),
            request.OperationId);

        var response = await _dispatcher.DispatchCommandAsync<CreateTransferCommand, IReadOnlyList<TransactionResponse>>(command, cancellationToken);
        return Created("/api/v1/transactions/transfers", response);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> ListAsync(
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] TransactionType? type,
        [FromQuery] TransactionStatus? status,
        [FromQuery] DateTime? competenceDateFrom,
        [FromQuery] DateTime? competenceDateTo,
        [FromQuery] DateTime? dueDateFrom,
        [FromQuery] DateTime? dueDateTo,
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery(Name = "_page")] int? legacyPage,
        [FromQuery(Name = "_size")] int? legacySize,
        CancellationToken cancellationToken = default)
    {
        var resolvedPage = page ?? legacyPage ?? 1;
        var resolvedSize = size ?? legacySize ?? 20;

        var query = new ListTransactionsQuery(
            accountId,
            categoryId,
            type,
            status,
            competenceDateFrom,
            competenceDateTo,
            dueDateFrom,
            dueDateTo,
            resolvedPage,
            resolvedSize);

        var response = await _dispatcher.DispatchQueryAsync<ListTransactionsQuery, PagedResult<TransactionResponse>>(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTransactionByIdQuery(id);
        var response = await _dispatcher.DispatchQueryAsync<GetTransactionByIdQuery, TransactionResponse>(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType<TransactionHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionHistoryResponse>> GetHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTransactionHistoryQuery(id);
        var response = await _dispatcher.DispatchQueryAsync<GetTransactionHistoryQuery, TransactionHistoryResponse>(query, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/adjustments")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> AdjustAsync(Guid id, [FromBody] AdjustTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new AdjustTransactionCommand(id, request.NewAmount, userId.ToString(), request.OperationId);
        var response = await _dispatcher.DispatchCommandAsync<AdjustTransactionCommand, TransactionResponse>(command, cancellationToken);

        return Created($"/api/v1/transactions/{response.Id}", response);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> CancelAsync(Guid id, [FromBody] CancelTransactionRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CancelTransactionCommand(id, userId.ToString(), request?.Reason, request?.OperationId);
        var response = await _dispatcher.DispatchCommandAsync<CancelTransactionCommand, TransactionResponse>(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("installment-groups/{groupId:guid}/cancel")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> CancelInstallmentGroupAsync(
        Guid groupId,
        [FromBody] CancelInstallmentGroupRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CancelInstallmentGroupCommand(groupId, userId.ToString(), request?.Reason, request?.OperationId);
        var response = await _dispatcher.DispatchCommandAsync<CancelInstallmentGroupCommand, IReadOnlyList<TransactionResponse>>(command, cancellationToken);

        return Ok(response);
    }
}
