using GestorFinanceiro.Financeiro.Application.Commands.Backup;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;
using GestorFinanceiro.Financeiro.Application.Queries.Backup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Controllers;

[ApiController]
[Route("api/v1/backup")]
[Authorize(Policy = "AdminOnly")]
public class BackupController : ControllerBase
{
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ImportTimeout = TimeSpan.FromMinutes(5);
    private readonly IDispatcher _dispatcher;

    public BackupController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("export")]
    [ProducesResponseType<BackupExportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BackupExportDto>> ExportAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ExportTimeout);

        var result = await _dispatcher.DispatchQueryAsync<ExportBackupQuery, BackupExportDto>(new ExportBackupQuery(), cts.Token);
        Response.Headers.ContentDisposition = $"attachment; filename=\"backup_{result.ExportedAt:yyyyMMdd_HHmmss}.json\"";

        return Ok(result);
    }

    [HttpPost("import")]
    [ProducesResponseType<BackupImportSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BackupImportSummaryDto>> ImportAsync([FromBody] BackupDataDto data, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ImportTimeout);

        var command = new ImportBackupCommand(data);
        var result = await _dispatcher.DispatchCommandAsync<ImportBackupCommand, BackupImportSummaryDto>(command, cts.Token);

        return Ok(result);
    }
}
