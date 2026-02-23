using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class ReceiptItemTests
{
    [Fact]
    public void Create_DadosValidos_CriaItemComAuditoria()
    {
        var transactionId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var item = ReceiptItem.Create(
            transactionId,
            "ARROZ TIPO 1 5KG",
            "7891234567890",
            2.000m,
            "UN",
            25.90m,
            51.80m,
            1,
            "user-1");

        var after = DateTime.UtcNow;

        item.TransactionId.Should().Be(transactionId);
        item.Description.Should().Be("ARROZ TIPO 1 5KG");
        item.ProductCode.Should().Be("7891234567890");
        item.Quantity.Should().Be(2.000m);
        item.UnitOfMeasure.Should().Be("UN");
        item.UnitPrice.Should().Be(25.90m);
        item.TotalPrice.Should().Be(51.80m);
        item.ItemOrder.Should().Be(1);
        item.CreatedBy.Should().Be("user-1");
        item.CreatedAt.Should().BeOnOrAfter(before);
        item.CreatedAt.Should().BeOnOrBefore(after);
    }
}
