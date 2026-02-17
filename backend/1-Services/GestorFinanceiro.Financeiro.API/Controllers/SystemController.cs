using GestorFinanceiro.Financeiro.Application.Commands.System;
using GestorFinanceiro.Financeiro.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/system")]
[Authorize(Policy = "AdminOnly")]
public class SystemController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public SystemController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetAsync(CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchCommandAsync<ResetSystemCommand, Unit>(new ResetSystemCommand(), cancellationToken);
        return Ok(new { message = "Sistema resetado com sucesso!" });
    }
}
