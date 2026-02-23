using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class EstablishmentTests
{
    [Fact]
    public void Create_DadosValidos_CriaEstabelecimentoComAuditoria()
    {
        var transactionId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var establishment = Establishment.Create(
            transactionId,
            "SUPERMERCADO EXEMPLO LTDA",
            "12345678000190",
            "12345678901234567890123456789012345678901234",
            "user-1");

        var after = DateTime.UtcNow;

        establishment.TransactionId.Should().Be(transactionId);
        establishment.Name.Should().Be("SUPERMERCADO EXEMPLO LTDA");
        establishment.Cnpj.Should().Be("12345678000190");
        establishment.AccessKey.Should().Be("12345678901234567890123456789012345678901234");
        establishment.CreatedBy.Should().Be("user-1");
        establishment.CreatedAt.Should().BeOnOrAfter(before);
        establishment.CreatedAt.Should().BeOnOrBefore(after);
    }
}
