# Especificação Técnica — API Completa (Fase 2)

## Resumo Executivo

A Fase 2 expõe o Core Financeiro (Fase 1) como uma API REST segura via ASP.NET Core Web API. As decisões arquiteturais centrais são: (1) autenticação JWT Bearer com refresh token persistido em PostgreSQL; (2) controllers MVC por recurso, roteando para o Dispatcher CQRS já existente; (3) tratamento global de erros via `IExceptionHandler` com Problem Details (RFC 9457); (4) paginação offset-based padronizada; (5) backup JSON transacional com substituição completa; (6) visibilidade compartilhada — todos os membros da família veem todos os dados, com auditoria de autoria.

A estratégia de implementação preserva as camadas existentes (Domain, Application, Infra) e expande cada uma minimamente: nova entidade `User` + `RefreshToken` no Domain, novos commands/queries de autenticação e auditoria na Application, configurações EF Core e repositórios na Infra, e toda a superfície HTTP na camada Services (controllers, middleware, filtros, Swagger).

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌──────────────────────────────────────────────────────────────────┐
│  1-Services (API)                                                │
│  ├─ Controllers/  (Accounts, Categories, Transactions,           │
│  │                 Auth, Users, Audit, Backup)                   │
│  ├─ Middleware/   (GlobalExceptionHandler, JwtMiddleware)         │
│  ├─ Filters/     (ValidationActionFilter)                        │
│  └─ Program.cs   (DI, Auth, Swagger, CORS, Pipeline)            │
└──────────────────────────────────────────────────────────────────┘
                          │  IDispatcher
                          ▼
┌──────────────────────────────────────────────────────────────────┐
│  2-Application                                                    │
│  ├─ Commands/   (existentes + Auth, User, Backup)                │
│  ├─ Queries/    (existentes + Auditoria, Paginação, Histórico)   │
│  ├─ Dtos/       (existentes + Auth, User, Audit, Paginação)     │
│  ├─ Validators/ (existentes + Auth, User)                        │
│  └─ Services/   (IPasswordHasher, ITokenService — interfaces)    │
└──────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌──────────────────────────────────────────────────────────────────┐
│  3-Domain                                                         │
│  ├─ Entity/     (existentes + User, RefreshToken)                │
│  ├─ Enum/       (existentes + UserRole)                          │
│  ├─ Interface/  (existentes + IUserRepository,                   │
│  │               IRefreshTokenRepository)                         │
│  └─ Exception/  (existentes + Auth exceptions)                   │
└──────────────────────────────────────────────────────────────────┘
                          ▲
                          │
┌──────────────────────────────────────────────────────────────────┐
│  4-Infra                                                          │
│  ├─ Context/    (FinanceiroDbContext + novos DbSets)             │
│  ├─ Repository/ (existentes + UserRepository,                    │
│  │               RefreshTokenRepository)                          │
│  ├─ Config/     (existentes + UserConfiguration,                 │
│  │               RefreshTokenConfiguration)                       │
│  ├─ Auth/       (PasswordHasher, TokenService)                   │
│  └─ Migrations/ (nova migration para users + refresh_tokens)     │
└──────────────────────────────────────────────────────────────────┘
```

**Fluxo HTTP**: Request → CORS → Authentication → Authorization → Controller → IDispatcher → Command/Query Handler → Domain/Infra → Response (com GlobalExceptionHandler interceptando exceções).

**Regra de dependência mantida**: Domain → zero deps. Application → Domain. Infra → Domain. Services → Application + Infra (para DI registration).

---

## Design de Implementação

### Interfaces Principais

```csharp
// === Application Layer — Auth Abstractions ===

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
    ClaimsPrincipal? ValidateAccessToken(string token);
}
```

```csharp
// === Domain Layer — Novos Repositórios ===

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct);
    Task RevokeByUserIdAsync(Guid userId, CancellationToken ct);
    Task CleanupExpiredAsync(CancellationToken ct);
}
```

```csharp
// === Application Layer — Paginação ===

public record PagedResult<T>(
    IEnumerable<T> Data,
    PaginationMetadata Pagination
);

public record PaginationMetadata(
    int Page,
    int Size,
    int Total,
    int TotalPages
);
```

### Modelos de Dados

#### Novas Entidades de Domínio

```csharp
// === User ===
public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool MustChangePassword { get; private set; } = true;

    public static User Create(string name, string email, string passwordHash,
        UserRole role, string createdByUserId) { ... }

    public void Deactivate(string userId) { ... }
    public void Activate(string userId) { ... }
    public void ChangePassword(string newPasswordHash, string userId) { ... }
}

// === RefreshToken ===
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(Guid userId, string token,
        DateTime expiresAt) { ... }

    public void Revoke() { ... }
}

// === Enum ===
public enum UserRole
{
    Admin = 1,
    Member = 2
}
```

#### Esquema de Banco — Novas Tabelas

```sql
-- users
CREATE TABLE users (
    id              UUID PRIMARY KEY,
    name            VARCHAR(150) NOT NULL,
    email           VARCHAR(255) NOT NULL,
    password_hash   VARCHAR(500) NOT NULL,
    role            INTEGER NOT NULL,          -- 1=Admin, 2=Member
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    must_change_password BOOLEAN NOT NULL DEFAULT TRUE,
    created_by      VARCHAR(100) NOT NULL,
    created_at      TIMESTAMP NOT NULL,
    updated_by      VARCHAR(100),
    updated_at      TIMESTAMP
);
CREATE UNIQUE INDEX ix_users_email ON users (email);

-- refresh_tokens
CREATE TABLE refresh_tokens (
    id              UUID PRIMARY KEY,
    user_id         UUID NOT NULL REFERENCES users(id),
    token           VARCHAR(500) NOT NULL,
    expires_at      TIMESTAMP NOT NULL,
    is_revoked      BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at      TIMESTAMP,
    created_by      VARCHAR(100) NOT NULL,
    created_at      TIMESTAMP NOT NULL,
    updated_by      VARCHAR(100),
    updated_at      TIMESTAMP
);
CREATE UNIQUE INDEX ix_refresh_tokens_token ON refresh_tokens (token);
CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
```

### Endpoints de API

Todos os endpoints seguem `rules/restful.md`: recursos em inglês/plural, kebab-case, versionamento via path (`/api/v1/...`), respostas de erro RFC 9457, paginação com `_page`/`_size`.

#### F1 — Autenticação e Gestão de Usuários

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `POST` | `/api/v1/auth/login` | Login com e-mail e senha, retorna access + refresh token | Público |
| `POST` | `/api/v1/auth/refresh` | Renova access token usando refresh token | Público |
| `POST` | `/api/v1/auth/logout` | Revoga refresh token do usuário | Autenticado |
| `POST` | `/api/v1/auth/change-password` | Altera senha do próprio usuário | Autenticado |
| `POST` | `/api/v1/users` | Criar novo usuário (nome, email, senha, role) | Admin |
| `GET`  | `/api/v1/users` | Listar usuários | Admin |
| `GET`  | `/api/v1/users/{id}` | Detalhe do usuário | Admin |
| `PATCH` | `/api/v1/users/{id}/status` | Ativar/desativar usuário | Admin |

**Exemplo de request/response — Login:**

```json
// POST /api/v1/auth/login
// Request:
{ "email": "admin@familia.com", "password": "SenhaSegura123!" }

// Response 200:
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "dGVzdC1y...",
  "expiresIn": 86400,
  "user": {
    "id": "guid",
    "name": "Admin",
    "email": "admin@familia.com",
    "role": "Admin"
  }
}
```

#### F2 — Endpoints de Contas

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `POST` | `/api/v1/accounts` | Criar conta | Autenticado |
| `GET`  | `/api/v1/accounts` | Listar contas (filtro: `isActive`) | Autenticado |
| `GET`  | `/api/v1/accounts/{id}` | Detalhe da conta | Autenticado |
| `PUT`  | `/api/v1/accounts/{id}` | Editar conta | Autenticado |
| `PATCH` | `/api/v1/accounts/{id}/status` | Ativar/desativar | Autenticado |

#### F3 — Endpoints de Categorias

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `POST` | `/api/v1/categories` | Criar categoria | Autenticado |
| `GET`  | `/api/v1/categories` | Listar categorias (filtro: `type`) | Autenticado |
| `PUT`  | `/api/v1/categories/{id}` | Editar nome | Autenticado |

#### F4 — Endpoints de Transações

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `POST` | `/api/v1/transactions` | Criar transação simples | Autenticado |
| `POST` | `/api/v1/transactions/installments` | Criar transação parcelada | Autenticado |
| `POST` | `/api/v1/transactions/recurrences` | Criar transação recorrente | Autenticado |
| `POST` | `/api/v1/transactions/transfers` | Criar transferência | Autenticado |
| `GET`  | `/api/v1/transactions` | Listar com filtros + paginação | Autenticado |
| `GET`  | `/api/v1/transactions/{id}` | Detalhe da transação | Autenticado |
| `POST` | `/api/v1/transactions/{id}/adjustments` | Criar ajuste | Autenticado |
| `POST` | `/api/v1/transactions/{id}/cancel` | Cancelar transação | Autenticado |
| `POST` | `/api/v1/transactions/installment-groups/{groupId}/cancel` | Cancelar grupo de parcelas | Autenticado |

**Filtros em listagem**: `accountId`, `categoryId`, `type`, `status`, `competenceDateFrom`, `competenceDateTo`, `dueDateFrom`, `dueDateTo`.
**Paginação**: `_page` (default 1), `_size` (default 20, max 100).

#### F5 — Histórico e Auditoria

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `GET`  | `/api/v1/transactions/{id}/history` | Histórico da transação (original + ajustes + cancelamento) | Autenticado |
| `GET`  | `/api/v1/audit` | Log de auditoria com filtros | Admin |

**Filtros auditoria**: `entityType`, `entityId`, `userId`, `dateFrom`, `dateTo`.

**Implementação do histórico**: O histórico de uma transação é composto por consulta ao campo `OriginalTransactionId` — a transação original + todas as transações de ajuste que referenciam o mesmo `OriginalTransactionId` — apresentados em ordem cronológica. Cancelamentos são representados pelo estado `Cancelled` da transação.

**Implementação da auditoria**: A auditoria utiliza os campos `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt` já existentes no `BaseEntity`. Para o endpoint `/api/v1/audit`, será criada uma nova tabela `audit_logs` com campos: `Id`, `EntityType`, `EntityId`, `Action` (Created/Updated/Deactivated/Cancelled), `UserId`, `Timestamp`, `PreviousData` (JSONB nullable). Os registros são inseridos nos command handlers existentes.

#### F6 — Backup Manual

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `GET`  | `/api/v1/backup/export` | Exportar JSON com todos os dados | Admin |
| `POST` | `/api/v1/backup/import` | Importar JSON, substituição completa | Admin |

**Formato do export**:

```json
{
  "exportedAt": "2026-02-14T10:00:00Z",
  "version": "1.0",
  "data": {
    "users": [ ... ],        // sem password_hash
    "accounts": [ ... ],
    "categories": [ ... ],
    "transactions": [ ... ],
    "recurrenceTemplates": [ ... ]
  }
}
```

**Fluxo do import**:
1. Validar formato e integridade referencial do JSON
2. Abrir transação
3. Truncar todas as tabelas na ordem correta (respeitando FKs)
4. Inserir dados na ordem: users → accounts → categories → recurrenceTemplates → transactions
5. Commit (rollback automático em caso de erro)

#### F7 — Health Check

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `GET`  | `/health` | Health check geral | Público |

---

## Pontos de Integração

Esta fase não possui integrações externas. Todas as dependências são internas:

- **PostgreSQL**: banco de dados existente da Fase 1, expandido com novas tabelas
- **Domain Services existentes**: `TransactionDomainService`, `InstallmentDomainService`, `RecurrenceDomainService`, `TransferDomainService` — consumidos sem alteração
- **CQRS Dispatcher existente**: Controllers delegam para `IDispatcher` existente

---

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
|---------------------|-----------------|---------------------------|----------------|
| `FinanceiroDbContext` | Mudança de Esquema | Adicionar DbSets para `User`, `RefreshToken`, `AuditLog`. Risco baixo — aditivo. | Nova migration EF Core |
| `BaseEntity` | Sem mudança | Auditoria (`CreatedBy`) passa a receber o `userId` do JWT em vez de string fixa. Risco baixo. | Propagar userId do token para commands |
| `Program.cs` | Mudança extensiva | De placeholder vazio para pipeline completa (DI, Auth, Swagger, CORS). Risco baixo — greenfield. | Implementar |
| `API .csproj` | Nova referência | Deve referenciar projeto Infra para registro de DI. Risco baixo. | Adicionar `ProjectReference` |
| `Application Commands` | Mudança compatível | Commands existentes (`CreateAccountCommand`, etc.) adicionam campo `UserId` para auditoria. Risco médio — requer atualização dos testes. | Refatorar commands e testes |
| `Application DI` | Extensão | Registrar novos handlers, validators, serviços de auth. Risco baixo. | Estender `ApplicationServiceExtensions` |
| `appsettings.json` | Extensão | Adicionar `ConnectionStrings`, `JwtSettings`, `CorsSettings`. Risco baixo. | Adicionar seções |

---

## Abordagem de Testes

### Testes Unitários

**Componentes a testar:**
- **Entidades `User` e `RefreshToken`**: factory methods, validações, transições de estado (ativar/desativar, revogar, expirar)
- **Novos Command/Query Handlers**: LoginCommandHandler, CreateUserCommandHandler, ExportBackupQueryHandler, etc.
- **Validators**: FluentValidation para todos os novos commands (login, create user, change password, filtros de transação)
- **TokenService**: geração e validação de JWT, geração de refresh token
- **PasswordHasher**: hash e verify

**Mocking**: Repositórios (IUserRepository, IRefreshTokenRepository) e ITokenService mockados com Moq. Nenhum mock de serviços externos (não há).

**Cenários críticos**:
- Login com credenciais válidas/inválidas/usuário inativo
- Refresh token expirado, revogado, reutilizado
- Criação de usuário com email duplicado
- Change password com senha incorreta atual
- Validação de todos os filtros de listagem de transações
- Export/Import: serialização fiel, validação de integridade

### Testes de Integração

**Componentes a testar juntos (via `WebApplicationFactory` + Testcontainers/PostgreSQL):**
- Fluxo login → obter token → chamar endpoint protegido → receber dados
- CRUD completo de contas, categorias e transações via HTTP
- Fluxo de refresh token (login → refresh → acesso)
- Permissões: membro tentando acessar endpoint admin (deve receber 403)
- Export → Import → verificar dados restaurados
- Paginação e filtros em listagem de transações
- Respostas de erro (400, 401, 403, 404) no formato RFC 9457

**Setup**: Reaproveitar a infra existente de `PostgreSqlFixture` e `DockerAvailableFactAttribute` dos testes de integração da Fase 1. Criar uma `CustomWebApplicationFactory` que registra o banco Testcontainers e executa migrations.

**Dados de seed**: Criar fixture que insere: 1 admin, 1 membro, contas, categorias e transações de exemplo.

### Testes End-to-End

Fora de escopo nesta fase. O projeto E2E existente permanece como placeholder.

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

1. **Entidades e Infraestrutura de Usuário** — `User`, `RefreshToken`, `UserRole`, exceções de auth, repositórios, EF Config, migration. Vem primeiro porque autenticação é pré-requisito de todos os endpoints.

2. **Autenticação (JWT + Refresh Token)** — `IPasswordHasher`, `ITokenService`, implementações na Infra, `LoginCommand`, `RefreshCommand`, `LogoutCommand`, `ChangePasswordCommand` + handlers e validators.

3. **Pipeline HTTP e tratamento de erros** — `Program.cs` com DI completa, JWT Bearer auth, `GlobalExceptionHandler` (RFC 9457), CORS, Swagger, `ValidationActionFilter`.

4. **Controllers de Auth e Users** — `AuthController`, `UsersController`. Primeiro endpoint testável ponta-a-ponta.

5. **Controllers de Contas e Categorias** — `AccountsController`, `CategoriesController`. Reutilizam commands/queries existentes da Fase 1, adicionando apenas a camada HTTP.

6. **Controller de Transações** — `TransactionsController`. Maior controller, com múltiplos verbos, filtros e paginação. Requer novos queries com suporte a filtros e paginação.

7. **Histórico e Auditoria** — `AuditLog` entity + config, inserção de audit nos handlers existentes, `TransactionHistoryQuery`, `AuditController`.

8. **Backup (Export/Import)** — `BackupController`, `ExportBackupQuery`, `ImportBackupCommand`. Último por ser isolado e não bloquear funcionalidades anteriores.

9. **Testes de Integração HTTP** — `CustomWebApplicationFactory`, testes de fluxo completo para cada controller.

### Dependências Técnicas

- **EF Core migrations**: devem ser aplicadas antes de qualquer teste de integração
- **Seed de admin inicial**: a migration ou o `Program.cs` deve garantir que ao menos 1 usuário admin exista (seed via migration ou endpoint público de setup inicial)
- **Testcontainers + Docker**: necessário para testes de integração (skip automático quando indisponível via `DockerAvailableFactAttribute` existente)

---

## Monitoramento e Observabilidade

### Health Check

- Endpoint `/health` com `AspNetCore.Diagnostics.HealthChecks`
- Checks: aplicação (self) + banco PostgreSQL (EF Core DbContext check)
- Disponível sem autenticação

### Logging Estruturado

Seguir `rules/dotnet-logging.md`:
- JSON estruturado com `service.name = "financeiro"`
- Campos `trace_id`, `span_id` quando disponíveis
- Scoped logging com `userId`, `request_path`, `http_method` em cada request
- Log de auditoria: `LogInformation` para operações CRUD bem-sucedidas
- Log de autenticação: `LogWarning` para login falho, `LogInformation` para login/logout bem-sucedido
- Nunca logar: senhas, tokens completos, dados pessoais sensíveis

### Métricas (Preparação)

Configurar OpenTelemetry conforme `rules/dotnet-logging.md` para tracing de:
- Requests HTTP (ASP.NET Core instrumentation)
- Queries EF Core
- Endpoint de exportação configurado como slow-endpoint (timeout estendido)

---

## Considerações Técnicas

### Decisões Principais

| Decisão | Escolha | Justificativa |
|---------|---------|---------------|
| **Autenticação** | JWT Bearer + Refresh Token em banco | Permite invalidação seletiva (logout, desativar usuário), rotação segura de tokens, sem dependência de cache externo |
| **Hash de senhas** | BCrypt (via `BCrypt.Net-Next`) | Padrão maduro, custo computacional configurável, amplamente auditado. Argon2 é alternativa futura se necessário |
| **Controllers vs Minimal APIs** | Controllers MVC (`[ApiController]`) | Melhor organização para API com muitos endpoints, atributos de autorização declarativos, integração nativa com Swagger |
| **Paginação** | Offset-based (`_page`, `_size`) | Conforme `rules/restful.md`, simples de implementar e consumir, adequado para volume de dados familiar |
| **Formato de erro** | RFC 9457 Problem Details | Conforme `rules/restful.md`, padronizado, integração nativa com ASP.NET Core 8 |
| **Backup import** | Substituição completa (truncate + insert) | Previsível, sem conflitos de merge, transacional. Adequado para uso familiar |
| **Isolamento de dados** | Visibilidade total entre membros | Contexto familiar — todos compartilham finanças. Auditoria registra autoria |
| **Auditoria** | Tabela `audit_logs` + campos `CreatedBy/UpdatedBy` existentes | Combina rastreabilidade granular com consulta via endpoint dedicado |
| **Versionamento de API** | Path-based (`/api/v1/`) | Conforme `rules/restful.md`, explícito e inequívoco |
| **Swagger** | Swashbuckle, habilitado apenas em Development | Segurança em produção, útil para testes locais |

### Riscos Conhecidos

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| Import de backup com volume grande causa timeout | Baixa (uso familiar) | Médio | Configurar timeout do endpoint de import para 5 min; validação prévia do JSON  |
| Refresh token leak permite acesso prolongado | Baixa | Alto | Rotação a cada uso, expiração de 7 dias, revogação no logout |
| Migration de banco quebra dados da Fase 1 | Baixa | Alto | Migration aditiva (ADD TABLE), sem ALTER em tabelas existentes. Testar com banco populado |
| Conflito de `CreatedBy` string com novo `userId` Guid | Média | Médio | Converter `CreatedBy` para usar `userId.ToString()` do JWT. Dados existentes da Fase 1 mantêm valor "system" |
| Swagger exposto acidentalmente em produção | Baixa | Baixo | Condicional `if (app.Environment.IsDevelopment())` |

### Requisitos Especiais

**Segurança:**
- Senhas: BCrypt com work factor ≥ 12
- JWT: chave simétrica (HMAC SHA-256) configurada via `appsettings.json` / variável de ambiente. Nunca commitada
- Refresh token: string aleatória criptograficamente segura (256 bits), armazenada com hash
- CORS: origens configuráveis via `appsettings.json`
- Rate limiting: fora de escopo (PRD), mas endpoint de login deve ter logging de tentativas falhas

**Performance:**
- Paginação com `Skip/Take` no EF Core (adequado para volume familiar)
- Export: `AsNoTracking()` para todas as queries de leitura
- Import: `BulkInsert` se disponível, ou `AddRange` + `SaveChanges` em batches

### Conformidade com Padrões

| Regra | Status | Observações |
|-------|--------|-------------|
| `rules/dotnet-architecture.md` | ✅ Conforme | Clean Architecture mantida, CQRS nativo, tratamento de erros com `IExceptionHandler` |
| `rules/dotnet-coding-standards.md` | ✅ Conforme | Código em inglês, PascalCase, camelCase, `_prefix` para campos privados, métodos ≤ 50 linhas |
| `rules/restful.md` | ✅ Conforme | Recursos em plural/inglês, kebab-case, versionamento path, RFC 9457, paginação `_page/_size` |
| `rules/dotnet-testing.md` | ✅ Conforme | xUnit + AwesomeAssertions + Moq, WebApplicationFactory + Testcontainers, AAA pattern |
| `rules/dotnet-logging.md` | ✅ Conforme | JSON estruturado, scoped logging, campos obrigatórios |
| `rules/dotnet-observability.md` | ✅ Conforme | Health checks com AspNetCore.Diagnostics.HealthChecks, CancellationToken em todas as async |
| `rules/dotnet-libraries-config.md` | ✅ Conforme | Mapster, FluentValidation, EF Core, Swashbuckle — todas bibliotecas aprovadas |

---

## Pacotes NuGet Adicionais (Fase 2)

```xml
<!-- API / Services -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="8.0.0" />

<!-- Infra (se não existirem) -->
<PackageReference Include="System.Text.Json" Version="8.0.0" />

<!-- Testes de Integração -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
```
