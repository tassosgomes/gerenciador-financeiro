using Testcontainers.PostgreSql;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
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

public static class PostgreSqlCollection
{
    public const string Name = "PostgreSqlIntegrationCollection";
}

[CollectionDefinition(PostgreSqlCollection.Name)]
public sealed class PostgreSqlCollectionDefinition : ICollectionFixture<PostgreSqlFixture>
{
}
