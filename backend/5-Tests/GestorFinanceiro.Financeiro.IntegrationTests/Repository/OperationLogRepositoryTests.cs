using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Repository;

[Collection(PostgreSqlCollection.Name)]
public sealed class OperationLogRepositoryTests : IntegrationTestBase
{
    public OperationLogRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task OperationLogRepository_CleanupExpired_RemoveExpirados()
    {
        var cancellationToken = CancellationToken.None;
        var repository = new OperationLogRepository(DbContext);

        var expiredLog = new OperationLog
        {
            OperationId = $"expired-{Guid.NewGuid()}",
            OperationType = "CreateTransaction",
            ResultEntityId = Guid.NewGuid(),
            ResultPayload = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        };

        var activeLog = new OperationLog
        {
            OperationId = $"active-{Guid.NewGuid()}",
            OperationType = "CreateTransaction",
            ResultEntityId = Guid.NewGuid(),
            ResultPayload = "{}",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12),
        };

        await repository.AddAsync(expiredLog, cancellationToken);
        await repository.AddAsync(activeLog, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        await repository.CleanupExpiredAsync(cancellationToken);

        var allLogs = await DbContext.OperationLogs.AsNoTracking().ToListAsync(cancellationToken);
        allLogs.Should().HaveCount(1);
        allLogs[0].OperationId.Should().Be(activeLog.OperationId);
    }
}
