using Testcontainers.PostgreSql;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("gestorfinanceiro_http_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        if (!DockerAvailabilityChecker.IsDockerAvailable)
        {
            return;
        }

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (!DockerAvailabilityChecker.IsDockerAvailable)
        {
            return;
        }

        await _container.DisposeAsync();
    }
}
