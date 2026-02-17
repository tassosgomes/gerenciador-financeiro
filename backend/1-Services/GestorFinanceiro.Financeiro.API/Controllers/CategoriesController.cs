using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.API.Extensions;
using GestorFinanceiro.Financeiro.Application.Commands.Category;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Category;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public CategoriesController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryResponse>> CreateAsync([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateCategoryCommand(request.Name, request.Type!.Value, userId.ToString());
        var response = await _dispatcher.DispatchCommandAsync<CreateCategoryCommand, CategoryResponse>(command, cancellationToken);

        return Created($"/api/v1/categories/{response.Id}", response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> ListAsync([FromQuery] CategoryType? type, CancellationToken cancellationToken)
    {
        var query = new ListCategoriesQuery(type);
        var response = await _dispatcher.DispatchQueryAsync<ListCategoriesQuery, IReadOnlyList<CategoryResponse>>(query, cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> UpdateAsync(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdateCategoryCommand(id, request.Name, userId.ToString());
        var response = await _dispatcher.DispatchCommandAsync<UpdateCategoryCommand, CategoryResponse>(command, cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, [FromQuery] Guid? migrateToCategoryId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new DeleteCategoryCommand(id, migrateToCategoryId, userId.ToString());
        await _dispatcher.DispatchCommandAsync<DeleteCategoryCommand, Unit>(command, cancellationToken);

        return NoContent();
    }
}
