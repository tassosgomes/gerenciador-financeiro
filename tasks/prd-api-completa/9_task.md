```markdown
---
status: pending
parallelizable: false
blocked_by: ["4.0", "5.0", "6.0", "7.0", "8.0"]
---

<task_context>
<domain>engine/testes</domain>
<type>testing</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database, http_server</dependencies>
<unblocks>""</unblocks>
</task_context>

# Tarefa 9.0: Testes de Integração HTTP

## Visão Geral

Implementar testes de integração completos para todos os controllers da API, usando `WebApplicationFactory<Program>` + Testcontainers com PostgreSQL. Os testes validam o fluxo ponta-a-ponta: request HTTP → controller → dispatcher → handler → repositório → banco → response HTTP, incluindo autenticação, autorização, validações e tratamento de erros.

Reutilizar a infraestrutura de teste existente da Fase 1 (`PostgreSqlFixture`, `DockerAvailableFactAttribute`) e criar uma `CustomWebApplicationFactory` que substitui a connection string pelo banco do Testcontainers.

## Requisitos

- Techspec: Testes de integração com `WebApplicationFactory` + Testcontainers
- `rules/dotnet-testing.md`: xUnit + AwesomeAssertions + Moq, AAA pattern
- Techspec: Skip automático quando Docker indisponível via `DockerAvailableFactAttribute`
- PRD F7 req 39-44: Validar respostas de erro (400, 401, 403, 404, 500) no formato RFC 9457

## Subtarefas

### Infraestrutura de Teste

- [ ] 9.1 Criar `CustomWebApplicationFactory` em `5-Tests/IntegrationTests/Base/`:
  - Herdar `WebApplicationFactory<Program>`
  - Substituir `ConfigureWebHost` para:
    - Remover DbContext registration existente
    - Registrar DbContext com connection string do Testcontainers
    - Executar migrations (`context.Database.MigrateAsync()`)
    - Configurar JWT com chave de teste fixa
    - Desabilitar Swagger e health checks externos se necessário
- [ ] 9.2 Criar `IntegrationTestBase` em `5-Tests/IntegrationTests/Base/`:
  - Classe base para todos os testes de integração HTTP
  - Propriedades: `HttpClient`, `Factory`
  - Helper para obter `HttpClient` autenticado (com JWT de teste)
  - Helper `AuthenticateAsAdmin()` → faz login e retorna client com Bearer token
  - Helper `AuthenticateAsMember()` → idem para membro
  - Cleanup do banco entre testes (ou uso de transaction rollback)
- [ ] 9.3 Criar seed de dados de teste em `5-Tests/IntegrationTests/Seed/`:
  - 1 usuário admin (email: `admin@test.com`, senha: `Admin123!`)
  - 1 usuário membro (email: `member@test.com`, senha: `Member123!`)
  - 2 contas (corrente e poupança)
  - 3 categorias (1 receita, 2 despesa)
  - Transações de exemplo (variados tipos e status)
- [ ] 9.4 Adicionar pacote `Microsoft.AspNetCore.Mvc.Testing` ao projeto de testes de integração
- [ ] 9.5 Garantir que `Program.cs` tenha `public partial class Program { }` ou equivalente para acesso do `WebApplicationFactory`

### Testes de Autenticação (AuthController)

- [ ] 9.6 Testes de login:
  - Login com credenciais válidas → 200 + tokens
  - Login com email inexistente → 401
  - Login com senha incorreta → 401
  - Login com usuário inativo → 401
  - Login sem body → 400
  - Login com email inválido → 400
- [ ] 9.7 Testes de refresh token:
  - Refresh com token válido → 200 + novos tokens
  - Refresh com token expirado → 401
  - Refresh com token inválido → 401
  - Refresh com token revogado → 401
- [ ] 9.8 Testes de logout:
  - Logout autenticado → 204
  - Logout sem token → 401
- [ ] 9.9 Testes de change password:
  - Change password com senha válida → 204
  - Change password com senha atual incorreta → 400/401
  - Change password com nova senha fraca → 400
  - Change password sem autenticação → 401

### Testes de Usuários (UsersController)

- [ ] 9.10 Testes de criação:
  - Admin cria usuário → 201 + Location header
  - Admin cria usuário com email duplicado → 400
  - Membro tenta criar usuário → 403
  - Não autenticado → 401
- [ ] 9.11 Testes de listagem:
  - Admin lista usuários → 200 + lista
  - Membro tenta listar → 403
- [ ] 9.12 Testes de detalhe:
  - Admin busca usuário existente → 200
  - Admin busca usuário inexistente → 404
- [ ] 9.13 Testes de status:
  - Admin desativa usuário → 204
  - Admin ativa usuário → 204
  - Membro tenta alterar status → 403

### Testes de Contas (AccountsController)

- [ ] 9.14 Testes de CRUD:
  - Criar conta → 201 + dados corretos
  - Listar contas → 200 + todas as contas
  - Listar contas com filtro isActive=true → apenas ativas
  - Detalhe de conta → 200 + saldo atual
  - Editar conta → 200 + dados atualizados
  - Desativar conta → 204
  - Ativar conta → 204
- [ ] 9.15 Testes de validação:
  - Criar conta sem nome → 400
  - Criar conta com nome duplicado → 400
  - Conta não encontrada → 404
- [ ] 9.16 Testes de autorização:
  - Sem token → 401

### Testes de Categorias (CategoriesController)

- [ ] 9.17 Testes de CRUD:
  - Criar categoria → 201
  - Listar categorias → 200
  - Listar categorias com filtro type=Income → apenas receitas
  - Editar categoria → 200
- [ ] 9.18 Testes de validação:
  - Criar categoria sem nome → 400
  - Criar categoria com nome duplicado → 400

### Testes de Transações (TransactionsController)

- [ ] 9.19 Testes de criação:
  - Criar transação simples → 201
  - Criar transação parcelada → 201 + N parcelas
  - Criar transferência → 201 + par de transações
  - Criar recorrência → 201
- [ ] 9.20 Testes de listagem com filtros:
  - Listar sem filtros → paginado (default 20)
  - Filtrar por accountId → apenas transações da conta
  - Filtrar por categoryId → apenas transações da categoria
  - Filtrar por período de competência
  - Filtrar por status
  - Paginação: page=2 com 5 items → metadados corretos
- [ ] 9.21 Testes de ações:
  - Ajustar transação → 201 + transação de ajuste
  - Cancelar transação → 200 + status Cancelled
  - Cancelar grupo de parcelas → 200 + parcelas canceladas
- [ ] 9.22 Testes de validação:
  - Criar transação com valor negativo → 400
  - Criar transação com conta inexistente → 400/404
  - Criar transação com conta inativa → 400
  - Cancelar transação já cancelada → 400

### Testes de Histórico e Auditoria

- [ ] 9.23 Testes de histórico:
  - Histórico de transação sem ajustes → 1 entry
  - Criar transação → ajustar → histórico mostra 2 entries
  - Criar transação → ajustar → cancelar → histórico mostra 3 entries
- [ ] 9.24 Testes de auditoria:
  - Admin consulta auditoria → 200 + registros
  - Filtrar por entityType → apenas registros do tipo
  - Filtrar por período → apenas registros do período
  - Membro tenta consultar → 403

### Testes de Backup

- [ ] 9.25 Testes de export:
  - Admin exporta → 200 + JSON completo
  - Export não inclui password_hash
  - Membro tenta exportar → 403
- [ ] 9.26 Testes de import:
  - Admin importa JSON válido → 200 + dados restaurados
  - Export → Import → Export → comparar (round-trip)
  - Import com referência inválida → 400
  - Membro tenta importar → 403

### Testes de Fluxo Completo

- [ ] 9.27 Fluxo auth completo:
  - Login → obter token → chamar endpoint protegido → receber dados → refresh → chamar de novo → logout
- [ ] 9.28 Fluxo CRUD completo:
  - Login → criar conta → criar categoria → criar transação → ajustar → cancelar → verificar histórico
- [ ] 9.29 Teste de respostas de erro RFC 9457:
  - Verificar que todas as respostas de erro contêm `type`, `title`, `status`, `detail`
  - Verificar Content-Type `application/problem+json`

### Testes de Health Check

- [ ] 9.30 Health check responde 200 sem autenticação
- [ ] 9.31 Health check reporta status do banco

### Validação

- [ ] 9.32 Todos os testes de integração passam com Docker disponível
- [ ] 9.33 Todos os testes são skippados corretamente quando Docker indisponível
- [ ] 9.34 Validar build completo com `dotnet build` e `dotnet test` a partir de `backend/`

## Sequenciamento

- Bloqueado por: 4.0, 5.0, 6.0, 7.0, 8.0 (todos os controllers e funcionalidades)
- Desbloqueia: Nenhum (última tarefa)
- Paralelizável: Não (depende de todas as tarefas anteriores)

## Detalhes de Implementação

### CustomWebApplicationFactory (exemplo)

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remover DbContext existente
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FinanceiroDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Registrar com Testcontainers
            services.AddDbContext<FinanceiroDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // JWT de teste
            services.Configure<JwtSettings>(opts =>
            {
                opts.SecretKey = "test-key-at-least-256-bits-long-for-hmac-sha256!";
                opts.Issuer = "test";
                opts.Audience = "test";
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        // Executar migrations + seed
    }

    public async Task DisposeAsync() => await _dbContainer.DisposeAsync();
}
```

### IntegrationTestBase (exemplo)

```csharp
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory Factory;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<HttpClient> AuthenticateAsAdminAsync()
    {
        var loginRequest = new { email = "admin@test.com", password = "Admin123!" };
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.AccessToken);
        return Client;
    }

    protected async Task<HttpClient> AuthenticateAsMemberAsync() { /* ... */ }
}
```

### Padrão de Teste AAA

```csharp
[DockerAvailableFact]
public async Task Create_Account_Returns_201_With_Location()
{
    // Arrange
    var client = await AuthenticateAsAdminAsync();
    var request = new
    {
        name = "Conta Corrente",
        type = "Checking",
        initialBalance = 1000.00m,
        allowNegativeBalance = false
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/v1/accounts", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    response.Headers.Location.Should().NotBeNull();
    var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
    account!.Name.Should().Be("Conta Corrente");
    account.Balance.Should().Be(1000.00m);
}
```

## Critérios de Sucesso

- `CustomWebApplicationFactory` configurada com Testcontainers PostgreSQL
- Todos os testes de integração passam com Docker disponível
- Testes são skippados quando Docker indisponível (sem falha no build)
- Cobertura de cenários: sucesso, validação, autorização, erros
- Fluxos completos validam o sistema ponta-a-ponta
- Respostas de erro no formato RFC 9457 Problem Details
- Seed de teste consistente e reutilizável
- `dotnet test` executa sem erros a partir de `backend/`
```
