using GestorFinanceiro.Financeiro.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Infra.StartupTasks;

/// <summary>
/// Hosted service that orchestrates startup tasks execution with retry logic.
/// Ensures all startup tasks complete successfully before the application starts accepting traffic.
/// </summary>
public sealed class StartupTasksHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StartupTasksHostedService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    private const int DefaultMaxAttempts = 12;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);

    public StartupTasksHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<StartupTasksHostedService> logger,
        IHostApplicationLifetime applicationLifetime,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        int maxAttempts = DefaultMaxAttempts,
        TimeSpan? retryDelay = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _delayAsync = delayAsync ?? Task.Delay;
        _maxAttempts = maxAttempts;
        _retryDelay = retryDelay ?? DefaultRetryDelay;

        if (_maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be greater than zero.");
        }

        if (_retryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay cannot be negative.");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting application startup tasks orchestration");

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var startupTasks = scope.ServiceProvider.GetServices<IStartupTask>().ToArray();

                _logger.LogInformation(
                    "Startup orchestration attempt {Attempt}/{MaxAttempts} with {TaskCount} task(s)",
                    attempt,
                    _maxAttempts,
                    startupTasks.Length);

                foreach (var task in startupTasks)
                {
                    var taskName = task.GetType().Name;

                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        ["startup_task"] = taskName,
                        ["attempt"] = attempt
                    }))
                    {
                        _logger.LogInformation("Executing startup task {TaskName}", taskName);

                        await task.ExecuteAsync(cancellationToken);

                        _logger.LogInformation(
                            "Startup task {TaskName} completed successfully",
                            taskName);
                    }
                }

                _logger.LogInformation("All startup tasks completed successfully. Application is ready to accept traffic");
                return;
            }
            catch (Exception ex) when (attempt < _maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Startup task orchestration failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds} seconds...",
                    attempt,
                    _maxAttempts,
                    _retryDelay.TotalSeconds);

                await _delayAsync(_retryDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Startup task orchestration failed after {MaxAttempts} attempts. Application startup aborted",
                    _maxAttempts);

                _applicationLifetime.StopApplication();
                throw;
            }
        }

        _logger.LogError(
            "Startup task orchestration reached an unexpected end state after {MaxAttempts} attempts",
            _maxAttempts);

        _applicationLifetime.StopApplication();
        throw new InvalidOperationException("Startup tasks orchestration terminated unexpectedly.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
