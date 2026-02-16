using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class OperationLogTests
{
    [Fact]
    public void Constructor_InstanciacaoPadrao_DefineTtlDe24Horas()
    {
        var before = DateTime.UtcNow;

        var operationLog = new OperationLog();

        var after = DateTime.UtcNow;
        operationLog.Id.Should().NotBe(Guid.Empty);
        operationLog.CreatedAt.Should().BeOnOrAfter(before);
        operationLog.CreatedAt.Should().BeOnOrBefore(after);
        operationLog.ExpiresAt.Should().Be(operationLog.CreatedAt.AddHours(24));
    }
}
