using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Infra.StartupTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests;

public class StartupTasksHostedServiceTests
{
    private readonly Mock<ILogger<StartupTasksHostedService>> _loggerMock;
    private readonly Mock<IHostApplicationLifetime> _applicationLifetimeMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;

    public StartupTasksHostedServiceTests()
    {
        _loggerMock = new Mock<ILogger<StartupTasksHostedService>>();
        _applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
    }

    [Fact]
    public async Task StartAsync_ComTasksSucesso_ExecutaTodasEmOrdem()
    {
        // Arrange
        var executionOrder = new List<string>();
        var task1 = new TestStartupTask("Task1", executionOrder);
        var task2 = new TestStartupTask("Task2", executionOrder);
        var service = CreateService(new List<IStartupTask> { task1, task2 });

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        executionOrder.Should().Equal("Task1", "Task2");
    }

    [Fact]
    public async Task StartAsync_TaskFalhaERetry_TentaNovaERetorna()
    {
        // Arrange
        var attempts = 0;
        var taskMock = new Mock<IStartupTask>();
        taskMock
            .Setup(t => t.ExecuteAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new InvalidOperationException("First attempt failed");
                }
                return Task.CompletedTask;
            });

        var service = CreateService(
            new List<IStartupTask> { taskMock.Object },
            maxAttempts: 2,
            retryDelay: TimeSpan.Zero,
            delayAsync: (_, _) => Task.CompletedTask);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task StartAsync_TaskFalhaSempre_ParaAplicacao()
    {
        // Arrange
        var taskMock = new Mock<IStartupTask>();
        taskMock
            .Setup(t => t.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Persistent failure"));

        var service = CreateService(
            new List<IStartupTask> { taskMock.Object },
            maxAttempts: 2,
            retryDelay: TimeSpan.Zero,
            delayAsync: (_, _) => Task.CompletedTask);

        // Act & Assert
        var action = async () => await service.StartAsync(CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>();

        _applicationLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_SempreRetornaCompletedTask()
    {
        // Arrange
        var service = CreateService(new List<IStartupTask>());

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert - StopAsync should complete without throwing
        Assert.True(true);
    }

    private StartupTasksHostedService CreateService(
        IReadOnlyCollection<IStartupTask> tasks,
        int maxAttempts = 12,
        TimeSpan? retryDelay = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        serviceProviderMock
            .Setup(provider => provider.GetService(typeof(IEnumerable<IStartupTask>)))
            .Returns(tasks);

        scopeMock
            .Setup(scope => scope.ServiceProvider)
            .Returns(serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(factory => factory.CreateScope())
            .Returns(scopeMock.Object);

        return new StartupTasksHostedService(
            _scopeFactoryMock.Object,
            _loggerMock.Object,
            _applicationLifetimeMock.Object,
            delayAsync,
            maxAttempts,
            retryDelay);
    }

    private class TestStartupTask : IStartupTask
    {
        private readonly string _name;
        private readonly List<string> _executionOrder;

        public TestStartupTask(string name, List<string> executionOrder)
        {
            _name = name;
            _executionOrder = executionOrder;
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _executionOrder.Add(_name);
            return Task.CompletedTask;
        }
    }
}
