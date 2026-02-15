using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public DashboardController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Get dashboard summary with total balance, monthly income, monthly expenses, and credit card debt
    /// </summary>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year (e.g., 2026)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard summary data</returns>
    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummaryAsync(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken = default)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid month parameter",
                Detail = "Month must be between 1 and 12"
            });
        }

        if (year < 2000 || year > 2100)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid year parameter",
                Detail = "Year must be between 2000 and 2100"
            });
        }

        var query = new GetDashboardSummaryQuery(month, year);
        var response = await _dispatcher.DispatchQueryAsync<GetDashboardSummaryQuery, DashboardSummaryResponse>(
            query,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get dashboard charts data with revenue vs expense for last 6 months and expenses by category
    /// </summary>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year (e.g., 2026)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard charts data</returns>
    [HttpGet("charts")]
    [ProducesResponseType<DashboardChartsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardChartsResponse>> GetChartsAsync(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken = default)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid month parameter",
                Detail = "Month must be between 1 and 12"
            });
        }

        if (year < 2000 || year > 2100)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid year parameter",
                Detail = "Year must be between 2000 and 2100"
            });
        }

        var query = new GetDashboardChartsQuery(month, year);
        var response = await _dispatcher.DispatchQueryAsync<GetDashboardChartsQuery, DashboardChartsResponse>(
            query,
            cancellationToken);

        return Ok(response);
    }
}
