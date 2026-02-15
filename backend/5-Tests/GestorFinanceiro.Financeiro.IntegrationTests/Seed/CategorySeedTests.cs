using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Seed;

[Collection(PostgreSqlCollection.Name)]
public sealed class CategorySeedTests : IntegrationTestBase
{
    private const string InitialCreateMigration = "20260214142740_InitialCreate";

    public CategorySeedTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        if (!DockerAvailabilityChecker.IsDockerAvailable)
        {
            return;
        }

        var cancellationToken = CancellationToken.None;
        var migrator = DbContext.Database.GetService<IMigrator>();

        await DbContext.Database.MigrateAsync(cancellationToken);
        await migrator.MigrateAsync(InitialCreateMigration, cancellationToken);
        await CleanDatabaseAsync(cancellationToken);
        await migrator.MigrateAsync(targetMigration: null, cancellationToken);
    }

    [DockerAvailableFact]
    public async Task Seed_CategoriasDefault_CriadasCorretamente()
    {
        var cancellationToken = CancellationToken.None;
        var expectedCreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var expectedExpenseCategories = new Dictionary<Guid, string>
        {
            [new Guid("00000000-0000-0000-0000-000000000001")] = "Alimentação",
            [new Guid("00000000-0000-0000-0000-000000000002")] = "Transporte",
            [new Guid("00000000-0000-0000-0000-000000000003")] = "Moradia",
            [new Guid("00000000-0000-0000-0000-000000000004")] = "Lazer",
            [new Guid("00000000-0000-0000-0000-000000000005")] = "Saúde",
            [new Guid("00000000-0000-0000-0000-000000000006")] = "Educação",
            [new Guid("00000000-0000-0000-0000-000000000007")] = "Vestuário",
            [new Guid("00000000-0000-0000-0000-000000000008")] = "Outros",
            [new Guid("00000000-0000-0000-0000-000000000013")] = "Serviços",
            [new Guid("00000000-0000-0000-0000-000000000014")] = "Impostos",
        };

        var expectedIncomeCategories = new Dictionary<Guid, string>
        {
            [new Guid("00000000-0000-0000-0000-000000000009")] = "Salário",
            [new Guid("00000000-0000-0000-0000-000000000010")] = "Freelance",
            [new Guid("00000000-0000-0000-0000-000000000011")] = "Investimentos",
            [new Guid("00000000-0000-0000-0000-000000000012")] = "Outros",
        };

        var categories = await DbContext.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var expenseCategories = categories
            .Where(category => category.Type == CategoryType.Despesa)
            .ToList();

        var incomeCategories = categories
            .Where(category => category.Type == CategoryType.Receita)
            .ToList();

        categories.Should().HaveCount(14);
        expenseCategories.Should().HaveCount(10);
        incomeCategories.Should().HaveCount(4);
        categories.Should().OnlyContain(category => category.IsActive);
        categories.Should().OnlyContain(category => category.IsSystem);
        categories.Should().OnlyContain(category => category.CreatedBy == "system");
        categories.Should().OnlyContain(category => category.CreatedAt.Kind == DateTimeKind.Utc);
        categories.Should().OnlyContain(category => category.CreatedAt == expectedCreatedAtUtc);

        expenseCategories
            .ToDictionary(category => category.Id, category => category.Name)
            .Should()
            .BeEquivalentTo(expectedExpenseCategories);

        incomeCategories
            .ToDictionary(category => category.Id, category => category.Name)
            .Should()
            .BeEquivalentTo(expectedIncomeCategories);

        categories.Select(category => category.Id)
            .Should()
            .BeEquivalentTo(expectedExpenseCategories.Keys.Concat(expectedIncomeCategories.Keys));
    }

    [DockerAvailableFact(Skip = "Opcional da tarefa 10.15: teste de handler E2E adiado para nao introduzir acoplamento prematuro.")]
    public Task CreateTransactionHandler_FluxoCompleto_TransacaoPersistidaESaldoAtualizado()
    {
        // TODO(task-10.15): implementar fluxo handler -> banco real quando setup de DI do modulo estiver estabilizado.
        return Task.CompletedTask;
    }
}
