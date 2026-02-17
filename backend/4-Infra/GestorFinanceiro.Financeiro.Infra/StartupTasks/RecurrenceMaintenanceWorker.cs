using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Infra.StartupTasks;

public sealed class RecurrenceMaintenanceWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurrenceMaintenanceWorker> _logger;

    public RecurrenceMaintenanceWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurrenceMaintenanceWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var recurrenceTemplateRepository = scope.ServiceProvider.GetRequiredService<IRecurrenceTemplateRepository>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var templates = await recurrenceTemplateRepository.GetActiveTemplatesAsync(cancellationToken);
        var referenceDate = DateTime.UtcNow.Date;

        foreach (var template in templates)
        {
            var operationId = $"recurrence-maintenance:{template.Id}:{referenceDate:yyyyMMdd}";

            try
            {
                await dispatcher.DispatchCommandAsync<GenerateRecurrenceCommand, Unit>(
                    new GenerateRecurrenceCommand(template.Id, referenceDate, "system", operationId),
                    cancellationToken);
            }
            catch (DuplicateOperationException)
            {
                _logger.LogInformation(
                    "Recurrence maintenance already processed for template {TemplateId} on {ReferenceDate}",
                    template.Id,
                    referenceDate);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error maintaining recurrence template {TemplateId}", template.Id);
            }
        }
    }
}