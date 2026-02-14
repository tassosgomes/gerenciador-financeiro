using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Audit;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Infra.Audit;

public class AuditServiceTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        _sut = new AuditService(_auditLogRepository.Object);
    }

    [Fact]
    public async Task LogAsync_WithPreviousData_ShouldSerializeJson()
    {
        var capturedAuditLog = default(AuditLog);
        _auditLogRepository
            .Setup(mock => mock.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((auditLog, _) => capturedAuditLog = auditLog)
            .Returns(Task.CompletedTask);

        var previousData = new { Name = "Conta Principal", IsActive = true };

        await _sut.LogAsync("Account", Guid.NewGuid(), "Updated", "user-1", previousData, CancellationToken.None);

        capturedAuditLog.Should().NotBeNull();
        capturedAuditLog!.PreviousData.Should().Contain("\"name\":\"Conta Principal\"");
        capturedAuditLog.PreviousData.Should().Contain("\"isActive\":true");
    }

    [Fact]
    public async Task LogAsync_ShouldCallRepositoryWithExpectedData()
    {
        var entityId = Guid.NewGuid();

        await _sut.LogAsync("Transaction", entityId, "Cancelled", "user-2", null, CancellationToken.None);

        _auditLogRepository.Verify(
            mock => mock.AddAsync(
                It.Is<AuditLog>(auditLog =>
                    auditLog.EntityType == "Transaction"
                    && auditLog.EntityId == entityId
                    && auditLog.Action == "Cancelled"
                    && auditLog.UserId == "user-2"
                    && auditLog.PreviousData == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
