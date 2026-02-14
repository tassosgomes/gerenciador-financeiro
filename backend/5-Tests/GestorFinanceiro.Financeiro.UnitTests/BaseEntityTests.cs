using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.UnitTests;

public class BaseEntityTests
{
    [Fact]
    public void SetAuditOnCreate_WhenCalled_ShouldSetCreatedByAndCreatedAtInUtc()
    {
        var entity = new TestEntity();
        var before = DateTime.UtcNow;

        entity.SetAuditOnCreate("user-1");

        var after = DateTime.UtcNow;

        entity.CreatedBy.Should().Be("user-1");
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
        entity.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void SetAuditOnUpdate_WhenCalled_ShouldSetUpdatedByAndUpdatedAtInUtc()
    {
        var entity = new TestEntity();
        var before = DateTime.UtcNow;

        entity.SetAuditOnUpdate("user-2");

        var after = DateTime.UtcNow;

        entity.UpdatedBy.Should().Be("user-2");
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt!.Value.Should().BeOnOrAfter(before);
        entity.UpdatedAt!.Value.Should().BeOnOrBefore(after);
        entity.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    private sealed class TestEntity : BaseEntity
    {
    }
}
