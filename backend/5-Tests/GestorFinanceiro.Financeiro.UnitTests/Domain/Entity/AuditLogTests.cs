using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class AuditLogTests
{
    [Fact]
    public void Create_WithValidData_ShouldFillAllProperties()
    {
        var entityId = Guid.NewGuid();

        var auditLog = AuditLog.Create(
            "Transaction",
            entityId,
            "Updated",
            "user-1",
            "{\"amount\":100}");

        auditLog.Id.Should().NotBeEmpty();
        auditLog.EntityType.Should().Be("Transaction");
        auditLog.EntityId.Should().Be(entityId);
        auditLog.Action.Should().Be("Updated");
        auditLog.UserId.Should().Be("user-1");
        auditLog.PreviousData.Should().Be("{\"amount\":100}");
    }

    [Fact]
    public void Create_ShouldSetTimestampAutomatically()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var auditLog = AuditLog.Create("Account", Guid.NewGuid(), "Created", "user-1");

        var after = DateTime.UtcNow.AddSeconds(1);
        auditLog.Timestamp.Should().BeOnOrAfter(before);
        auditLog.Timestamp.Should().BeOnOrBefore(after);
    }
}
