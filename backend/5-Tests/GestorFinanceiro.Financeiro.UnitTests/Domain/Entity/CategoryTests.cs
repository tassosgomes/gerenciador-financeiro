using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class CategoryTests
{
    [Fact]
    public void Create_DadosValidos_CriaCategoriaComTipo()
    {
        var category = Category.Create("Alimentacao", CategoryType.Despesa, "user-1");

        category.Name.Should().Be("Alimentacao");
        category.Type.Should().Be(CategoryType.Despesa);
        category.IsActive.Should().BeTrue();
        category.IsSystem.Should().BeFalse();
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

    [Fact]
    public void UpdateName_CategoriaDoSistema_LancaExcecao()
    {
        var systemCategory = Category.Restore(
            Guid.NewGuid(),
            "Alimentação",
            CategoryType.Despesa,
            isActive: true,
            isSystem: true,
            "system",
            DateTime.UtcNow,
            null,
            null);

        var action = () => systemCategory.UpdateName("Novo Nome", "user-1");

        action.Should().Throw<SystemCategoryCannotBeChangedException>();
    }

    [Fact]
    public void Restore_ComIsSystem_RestauraCategoriaCorretamente()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var category = Category.Restore(
            id,
            "Salário",
            CategoryType.Receita,
            isActive: true,
            isSystem: true,
            "system",
            createdAt,
            null,
            null);

        category.Id.Should().Be(id);
        category.Name.Should().Be("Salário");
        category.Type.Should().Be(CategoryType.Receita);
        category.IsActive.Should().BeTrue();
        category.IsSystem.Should().BeTrue();
        category.CreatedBy.Should().Be("system");
        category.CreatedAt.Should().Be(createdAt);
    }
}
