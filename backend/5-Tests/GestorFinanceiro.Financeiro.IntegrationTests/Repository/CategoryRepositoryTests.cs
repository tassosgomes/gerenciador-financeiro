using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Model;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

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

    [DockerAvailableFact]
    public async Task CategoryRepository_HasLinkedDataAsync_RetornaTrueQuandoCategoriaVinculadaAoOrcamento()
    {
        var cancellationToken = CancellationToken.None;
        var category = await CreateCategoryAsync($"Categoria-Budget-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);
        var budgetRepository = new BudgetRepository(DbContext);
        var categoryRepository = new CategoryRepository(DbContext);

        var budget = Budget.Create(
            $"Orcamento-{Guid.NewGuid()}",
            10m,
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month,
            [category.Id],
            false,
            "integration-user");

        await budgetRepository.AddAsync(budget, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var hasLinkedData = await categoryRepository.HasLinkedDataAsync(category.Id, cancellationToken);

        hasLinkedData.Should().BeTrue();
    }

    [DockerAvailableFact]
    public async Task CategoryRepository_MigrateLinkedDataAsync_DeveRemoverVinculoDoBudgetCategories()
    {
        var cancellationToken = CancellationToken.None;
        var sourceCategory = await CreateCategoryAsync($"Categoria-Origem-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);
        var targetCategory = await CreateCategoryAsync($"Categoria-Destino-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);

        var budgetRepository = new BudgetRepository(DbContext);
        var categoryRepository = new CategoryRepository(DbContext);

        var budget = Budget.Create(
            $"Orcamento-Migracao-{Guid.NewGuid()}",
            15m,
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month,
            [sourceCategory.Id],
            false,
            "integration-user");

        await budgetRepository.AddAsync(budget, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        await categoryRepository.MigrateLinkedDataAsync(sourceCategory.Id, targetCategory.Id, "integration-user", cancellationToken);

        var sourceLinkExists = await DbContext.Set<BudgetCategoryLink>()
            .AsNoTracking()
            .AnyAsync(link => link.CategoryId == sourceCategory.Id, cancellationToken);

        sourceLinkExists.Should().BeFalse();
    }
}
