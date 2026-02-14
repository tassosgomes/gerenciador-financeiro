---
status: pending
parallelizable: false
blocked_by: ["8.0", "9.0"]
---

<task_context>
<domain>infra/testes</domain>
<type>testing</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database</dependencies>
<unblocks></unblocks>
</task_context>

# Tarefa 10.0: Testes de Integração

## Visão Geral

Criar testes de integração que validam a persistência real com PostgreSQL em container via Testcontainers. Estes testes verificam: migrations aplicam corretamente, repositories persistem/recuperam entidades, `SELECT FOR UPDATE` bloqueia efetivamente, UnitOfWork faz rollback, seed de categorias funciona, e o fluxo ponta a ponta dos handlers com banco real.

**Importante**: Os testes devem ser **pulados de forma limpa** quando o Docker engine não estiver disponível, sem quebrar o build — conforme `copilot-instructions.md`.

## Requisitos

- Techspec: xUnit + Testcontainers (PostgreSQL)
- `copilot-instructions.md`: testes de integração com containers devem rodar quando Docker disponível e pular limpo quando não
- PRD F10 req 43: verificar que `SELECT FOR UPDATE` efetivamente bloqueia linha concorrente
- PRD F10 req 44: verificar que UnitOfWork faz rollback em exceção

## Subtarefas

### Infraestrutura de testes

- [ ] 10.1 Criar `PostgreSqlFixture` — classe que gerencia o lifecycle do container PostgreSQL via Testcontainers
- [ ] 10.2 Criar `DockerAvailableFactAttribute` (ou mecanismo equivalente) para skip limpo quando Docker não está disponível
- [ ] 10.3 Criar `IntegrationTestBase` — classe base que inicializa `FinanceiroDbContext` apontando para o container, aplica migrations e limpa dados entre testes

### Testes de Migrations

- [ ] 10.4 `Migrations_AplicamCorretamente_SchemaCriado` — aplica todas as migrations e verifica que as tabelas existem

### Testes de Repositories

- [ ] 10.5 `AccountRepository_AddAndGetById_PersistERecuperaCorretamente`
- [ ] 10.6 `AccountRepository_GetByIdWithLock_RetornaContaComLock` (verificar que a query SQL executa dentro de transação)
- [ ] 10.7 `TransactionRepository_AddAndGetByInstallmentGroup_RetornaParcelas`
- [ ] 10.8 `TransactionRepository_GetByOperationId_RetornaTransacaoCorreta`
- [ ] 10.9 `CategoryRepository_ExistsByNameAndType_RetornaTrueSeExiste`
- [ ] 10.10 `OperationLogRepository_CleanupExpired_RemoveExpirados`

### Testes de Concorrência

- [ ] 10.11 `SelectForUpdate_DuasOperacoesParalelas_SegundaEsperaPrimeiraTerminar` — usar `Task.WhenAll` com duas tasks que tentam dar lock na mesma conta; verificar serialização

### Testes de UnitOfWork

- [ ] 10.12 `UnitOfWork_CommitAposOperacao_DadosPersistidos`
- [ ] 10.13 `UnitOfWork_RollbackAposExcecao_DadosNaoPersistidos`

### Testes de Seed

- [ ] 10.14 `Seed_CategoriasDefault_CriadasCorretamente` (após migration/seed, verificar que categorias padrão existem)

### Testes de fluxo E2E (handler → banco)

- [ ] 10.15 `CreateTransactionHandler_FluxoCompleto_TransacaoPersistidaESaldoAtualizado` (opcional — usa handler real com banco real)

## Sequenciamento

- Bloqueado por: 8.0 (Repositories), 9.0 (Application Handlers)
- Desbloqueia: Nenhum
- Paralelizável: Não (última tarefa funcional)

## Detalhes de Implementação

### Localização dos testes

```
5-Tests/GestorFinanceiro.Financeiro.IntegrationTests/
├── Fixtures/
│   ├── PostgreSqlFixture.cs
│   └── DockerAvailableFactAttribute.cs
├── Base/
│   └── IntegrationTestBase.cs
├── Repository/
│   ├── AccountRepositoryTests.cs
│   ├── TransactionRepositoryTests.cs
│   ├── CategoryRepositoryTests.cs
│   └── OperationLogRepositoryTests.cs
├── Concurrency/
│   └── SelectForUpdateTests.cs
├── UnitOfWork/
│   └── UnitOfWorkTests.cs
└── Seed/
    └── CategorySeedTests.cs
```

### PostgreSqlFixture

```csharp
public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

### Skip quando Docker não disponível

```csharp
public class DockerAvailableFactAttribute : FactAttribute
{
    public DockerAvailableFactAttribute()
    {
        if (!IsDockerAvailable())
        {
            Skip = "Docker não está disponível neste ambiente.";
        }
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
```

### Teste de SELECT FOR UPDATE

```csharp
[DockerAvailableFact]
public async Task SelectForUpdate_DuasOperacoesParalelas_SegundaEsperaPrimeiraTerminar()
{
    // Arrange: criar conta no banco
    // Act: lançar duas Tasks paralelas que:
    //   1. Abrem transação
    //   2. Fazem GetByIdWithLockAsync na mesma conta
    //   3. Aguardam 500ms (simular operação)
    //   4. Fazem commit
    // Assert: verificar que as operações foram serializadas (tempo total ≥ 1s)
}
```

### Teste de Rollback

```csharp
[DockerAvailableFact]
public async Task UnitOfWork_RollbackAposExcecao_DadosNaoPersistidos()
{
    // Arrange: criar conta
    // Act: iniciar transação → modificar saldo → rollback
    // Assert: saldo original permanece inalterado
}
```

### Observações

- **Todos** os testes de integração devem usar `DockerAvailableFactAttribute` em vez de `[Fact]`
- Usar `ICollectionFixture<PostgreSqlFixture>` para compartilhar o container entre testes (evitar criar N containers)
- Limpar dados entre testes para evitar interferência (truncate tables ou recriação do schema)
- Container PostgreSQL: usar imagem `postgres:16-alpine` (leve e rápida)

## Critérios de Sucesso

- Todos os testes de integração passam quando Docker está disponível
- Todos os testes são pulados limpo (skip) quando Docker **não** está disponível, sem quebrar o build
- `SELECT FOR UPDATE` efetivamente serializa operações concorrentes
- Rollback do UnitOfWork reverte mudanças
- Migrations aplicam corretamente e criam todas as tabelas
- Seed de categorias gera os registros esperados
- `dotnet test` passa sem falhas em qualquer ambiente
