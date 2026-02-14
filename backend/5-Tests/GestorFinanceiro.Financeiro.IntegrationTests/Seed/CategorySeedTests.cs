using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Seed;

[Collection(PostgreSqlCollection.Name)]
public sealed class CategorySeedTests : IntegrationTestBase
{
    public CategorySeedTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact(Skip = "Dependencia da tarefa 11.0: validar seed de categorias default apos implementacao do seeder.")]
    public Task Seed_CategoriasDefault_CriadasCorretamente()
    {
        // TODO(task-11): implementar validacao do seed padrao quando a tarefa de seed estiver concluida.
        return Task.CompletedTask;
    }

    [DockerAvailableFact(Skip = "Opcional da tarefa 10.15: teste de handler E2E adiado para nao introduzir acoplamento prematuro.")]
    public Task CreateTransactionHandler_FluxoCompleto_TransacaoPersistidaESaldoAtualizado()
    {
        // TODO(task-10.15): implementar fluxo handler -> banco real quando setup de DI do modulo estiver estabilizado.
        return Task.CompletedTask;
    }
}
