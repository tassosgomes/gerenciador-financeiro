```markdown
---
status: completed
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>infra/configuração</domain>
<type>implementation</type>
<scope>middleware</scope>
<complexity>high</complexity>
<dependencies>http_server</dependencies>
<unblocks>"4.0", "5.0", "6.0", "7.0", "8.0"</unblocks>
</task_context>

# Tarefa 3.0: Pipeline HTTP e Tratamento de Erros

## Visão Geral

Configurar a pipeline completa do ASP.NET Core Web API: DI de todos os serviços, autenticação JWT Bearer, CORS, Swagger/OpenAPI, tratamento global de erros com `IExceptionHandler` (RFC 9457 Problem Details), `ValidationActionFilter` para FluentValidation, e health check. Esta tarefa transforma o `Program.cs` placeholder em uma aplicação web funcional.

## Requisitos

- Techspec: `Program.cs` com DI completa, Auth, Swagger, CORS, Pipeline
- Techspec: `GlobalExceptionHandler` com Problem Details (RFC 9457)
- Techspec: Health check em `/health` (público)
- PRD F7 req 39-44: Tratamento padronizado de erros (400, 401, 403, 404, 500)
- `rules/restful.md`: Respostas de erro RFC 9457, versionamento via path `/api/v1/`
- `rules/dotnet-architecture.md`: `IExceptionHandler` para tratamento global
- `rules/dotnet-logging.md`: JSON estruturado, scoped logging
- `rules/dotnet-observability.md`: Health checks, OpenTelemetry

## Subtarefas

### Pacotes NuGet

- [ ] 3.1 Adicionar pacotes ao projeto API (.csproj):
  - `Swashbuckle.AspNetCore`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
  - `AspNetCore.HealthChecks.NpgSql`
- [ ] 3.2 Adicionar `ProjectReference` da API para o projeto Infra (necessário para registro de DI):
  - `<ProjectReference Include="..\..\4-Infra\GestorFinanceiro.Financeiro.Infra\GestorFinanceiro.Financeiro.Infra.csproj" />`

### Configuração (appsettings)

- [ ] 3.3 Atualizar `appsettings.json` com seções:
  - `ConnectionStrings.DefaultConnection` (PostgreSQL)
  - `JwtSettings` (SecretKey, Issuer, Audience, AccessTokenExpirationMinutes, RefreshTokenExpirationDays)
  - `CorsSettings` (AllowedOrigins como array)
- [ ] 3.4 Atualizar `appsettings.Development.json` com valores de desenvolvimento:
  - Connection string local PostgreSQL
  - JWT SecretKey de desenvolvimento (≥ 256 bits)
  - CORS permitindo `http://localhost:*`

### Tratamento Global de Erros

- [ ] 3.5 Criar `GlobalExceptionHandler` em `1-Services/Middleware/GlobalExceptionHandler.cs`:
  - Implementar `IExceptionHandler`
  - Mapear `DomainException` → 400 Bad Request
  - Mapear exceções de validação → 400 com detalhes dos campos
  - Mapear `InvalidCredentialsException` → 401 Unauthorized
  - Mapear `InactiveUserException` → 401 Unauthorized
  - Mapear `InvalidRefreshTokenException` → 401 Unauthorized
  - Mapear `UnauthorizedAccessException` → 403 Forbidden
  - Mapear entity not found exceptions → 404 Not Found
  - Mapear qualquer outra exceção → 500 Internal Server Error (sem stack trace em produção)
  - Formato RFC 9457 Problem Details com `type`, `title`, `status`, `detail`, `instance`
  - Logging: `LogWarning` para erros de domínio/negócio, `LogError` para erros inesperados

### Filtro de Validação

- [ ] 3.6 Criar `ValidationActionFilter` em `1-Services/Filters/ValidationActionFilter.cs`:
  - Interceptar `ModelState` inválido antes de chegar ao controller
  - Retornar 400 com Problem Details contendo detalhes de validação
  - Formato: `{ "type": "...", "title": "Validation Error", "status": 400, "errors": { "field": ["message"] } }`

### Pipeline (Program.cs)

- [ ] 3.7 Configurar DI completa no `Program.cs`:
  - Registrar serviços de infraestrutura (DbContext, repositórios, UnitOfWork) via extensão existente
  - Registrar serviços de aplicação (Dispatcher, handlers, validators) via extensão existente
  - Registrar serviços de autenticação (PasswordHasher, TokenService, JwtSettings)
  - Registrar `GlobalExceptionHandler`
  - Registrar controllers com `AddControllers()` e `ValidationActionFilter`
  - Suprimir o filtro de validação automático do ASP.NET (`SuppressModelStateInvalidFilter = true`)
- [ ] 3.8 Configurar autenticação JWT Bearer:
  - `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`
  - `AddJwtBearer()` com validação de issuer, audience, lifetime, signing key
  - Chave simétrica via `JwtSettings.SecretKey`
- [ ] 3.9 Configurar autorização:
  - Política `AdminOnly` requerendo role `Admin`
  - Política default requerendo autenticação
- [ ] 3.10 Configurar CORS:
  - Origens lidas de `CorsSettings.AllowedOrigins`
  - Permitir headers e métodos necessários
- [ ] 3.11 Configurar Swagger/OpenAPI:
  - Título: "GestorFinanceiro API"
  - Versão: "v1"
  - Definição de segurança JWT Bearer
  - Habilitar apenas em ambiente Development
- [ ] 3.12 Configurar Health Checks:
  - Self check
  - DbContext check (PostgreSQL)
  - Endpoint `/health` sem autenticação
- [ ] 3.13 Configurar pipeline de middleware na ordem correta:
  ```
  UseExceptionHandler → UseCors → UseAuthentication → UseAuthorization → MapControllers → MapHealthChecks
  ```
- [ ] 3.14 Configurar Swagger middleware (condicional em Development):
  ```
  UseSwagger → UseSwaggerUI
  ```

### Logging Estruturado

- [ ] 3.15 Configurar logging conforme `rules/dotnet-logging.md`:
  - JSON estruturado
  - Campos: `service.name = "financeiro"`, `trace_id`, `span_id`
  - Scoped logging com `userId`, `request_path`, `http_method`
  - Nunca logar senhas, tokens completos, dados sensíveis

### Seed de Admin Inicial

- [ ] 3.16 Implementar seed de admin padrão no startup (garantir que ao menos 1 admin exista):
  - Verificar se existe algum usuário no banco
  - Se não existir, criar admin padrão (email/senha configurável via appsettings)
  - Usar `IPasswordHasher` para hash da senha
  - Logar informação sobre seed (sem exibir senha)

### Testes

- [ ] 3.17 Teste unitário para `GlobalExceptionHandler`:
  - Mapeia `DomainException` para 400
  - Mapeia exceções de not found para 404
  - Mapeia exceções de auth para 401
  - Mapeia exceções genéricas para 500
  - Gera Problem Details no formato correto
- [ ] 3.18 Teste unitário para `ValidationActionFilter`:
  - ModelState inválido retorna 400 com erros formatados

### Validação

- [ ] 3.19 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: 1.0 (necessita das entidades e repositórios de usuário para DI)
- Desbloqueia: 4.0, 5.0, 6.0, 7.0, 8.0 (todos os controllers)
- Paralelizável: Sim (pode ser executada em paralelo com 2.0)

## Detalhes de Implementação

### Program.cs (estrutura final)

```csharp
var builder = WebApplication.CreateBuilder(args);

// DI
builder.Services.AddInfrastructure(builder.Configuration);    // DbContext, repos, UoW
builder.Services.AddApplication();                             // Dispatcher, handlers
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* ... */ });
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});
builder.Services.AddControllers(options => {
    options.Filters.Add<ValidationActionFilter>();
})
.ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = true);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(/* JWT security definition */);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FinanceiroDbContext>()
    .AddNpgSql(/* connection string */);
builder.Services.AddCors(/* ... */);

var app = builder.Build();

// Seed admin
using (var scope = app.Services.CreateScope()) { /* seed if no users */ }

// Middleware pipeline
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### GlobalExceptionHandler (mapeamento)

| Exceção | Status | Title |
|---------|--------|-------|
| `AccountNameAlreadyExistsException` | 400 | Nome de conta já existe |
| `CategoryNameAlreadyExistsException` | 400 | Nome de categoria já existe |
| `InsufficientBalanceException` | 400 | Saldo insuficiente |
| `InvalidTransactionAmountException` | 400 | Valor inválido |
| Outras `DomainException` | 400 | Erro de validação |
| `InvalidCredentialsException` | 401 | Credenciais inválidas |
| `InactiveUserException` | 401 | Usuário inativo |
| `InvalidRefreshTokenException` | 401 | Token inválido |
| `AccountNotFoundException` | 404 | Conta não encontrada |
| `CategoryNotFoundException` | 404 | Categoria não encontrada |
| `TransactionNotFoundException` | 404 | Transação não encontrada |
| `Exception` (genérico) | 500 | Erro interno |

### appsettings.json (novas seções)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=gestorfinanceiro;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "CHANGE_THIS_TO_A_SECURE_KEY_AT_LEAST_256_BITS_LONG",
    "Issuer": "GestorFinanceiro",
    "Audience": "GestorFinanceiro",
    "AccessTokenExpirationMinutes": 1440,
    "RefreshTokenExpirationDays": 7
  },
  "CorsSettings": {
    "AllowedOrigins": ["http://localhost:3000"]
  }
}
```

## Critérios de Sucesso

- `Program.cs` configura DI, Auth, CORS, Swagger e Health Check
- Swagger UI acessível em Development em `/swagger`
- Health check responde em `/health` sem autenticação
- `GlobalExceptionHandler` converte exceções do domínio em Problem Details corretos
- `ValidationActionFilter` captura erros de modelstate
- JWT Bearer authentication configurada e funcional
- Políticas de autorização (`AdminOnly`) configuradas
- Seed de admin criado no startup quando banco vazio
- Logging estruturado configurado
- Build compila sem erros
```
