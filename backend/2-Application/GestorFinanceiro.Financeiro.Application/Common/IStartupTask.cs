namespace GestorFinanceiro.Financeiro.Application.Common;

/// <summary>
/// Represents a task that runs during application startup before accepting traffic.
/// Startup tasks are executed in the order they are registered in DI.
/// </summary>
public interface IStartupTask
{
    /// <summary>
    /// Executes the startup task.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
