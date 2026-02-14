using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.API.Extensions;
using GestorFinanceiro.Financeiro.Application.Commands.User;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public UsersController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserResponse>> CreateAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateUserCommand(request.Name, request.Email, request.Password, request.Role, userId.ToString());
        var response = await _dispatcher.DispatchCommandAsync<CreateUserCommand, UserResponse>(command, cancellationToken);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = new GetAllUsersQuery();
        var response = await _dispatcher.DispatchQueryAsync<GetAllUsersQuery, IEnumerable<UserResponse>>(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var response = await _dispatcher.DispatchQueryAsync<GetUserByIdQuery, UserResponse>(query, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatusAsync(Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new ToggleUserStatusCommand(id, request.IsActive!.Value, userId.ToString());
        await _dispatcher.DispatchCommandAsync<ToggleUserStatusCommand, Unit>(command, cancellationToken);
        return NoContent();
    }
}
