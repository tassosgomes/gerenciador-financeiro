using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.UnitTests;

public class CategoryTests
{
    [Fact]
    public void Create_DadosValidos_CriaCategoriaComTipo()
    {
        var category = Category.Create("Alimentacao", CategoryType.Despesa, "user-1");

        category.Name.Should().Be("Alimentacao");
        category.Type.Should().Be(CategoryType.Despesa);
        category.IsActive.Should().BeTrue();
        category.CreatedBy.Should().Be("user-1");
    }

    [Fact]
    public void UpdateName_NovoNome_AtualizaNomeComAuditoria()
    {
        var category = Category.Create("Lazer", CategoryType.Despesa, "user-1");

        category.UpdateName("Entretenimento", "user-2");

        category.Name.Should().Be("Entretenimento");
        category.UpdatedBy.Should().Be("user-2");
        category.UpdatedAt.Should().NotBeNull();
    }
}
