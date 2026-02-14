using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Repository;

[Collection(PostgreSqlCollection.Name)]
public sealed class CategoryRepositoryTests : IntegrationTestBase
{
    public CategoryRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task CategoryRepository_ExistsByNameAndType_RetornaTrueSeExiste()
    {
        var cancellationToken = CancellationToken.None;
        var categoryName = $"Categoria-{Guid.NewGuid()}";
        var repository = new CategoryRepository(DbContext);

        await CreateCategoryAsync(categoryName, CategoryType.Despesa, cancellationToken);

        var categoryExists = await repository.ExistsByNameAndTypeAsync(categoryName, CategoryType.Despesa, cancellationToken);

        categoryExists.Should().BeTrue();
    }
}
