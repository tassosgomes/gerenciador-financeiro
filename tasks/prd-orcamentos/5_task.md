```markdown
---
status: pending
parallelizable: false
blocked_by: ["2.0", "3.0", "4.0"]
---

<task_context>
<domain>services/api</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"8.0", "9.0"</unblocks>
</task_context>

# Tarefa 5.0: API Layer — BudgetsController, Request DTOs e Registro DI

## Visão Geral

Implementar a camada de API da feature de Orçamentos: `BudgetsController` com 7 endpoints REST sob `api/v1/budgets`, Request DTOs para criação e edição, mapeamento das novas domain exceptions no `GlobalExceptionHandler` e consolidação de todos os registros DI necessários. Esta tarefa conecta todos os componentes das camadas anteriores (Domain, Infra, Application) e expõe a API funcional para o frontend.

## Requisitos

- Techspec: 7 endpoints REST sob `api/v1/budgets`
- Techspec: Request DTOs `CreateBudgetRequest` e `UpdateBudgetRequest`
- Techspec: Todos os endpoints requerem `[Authorize]`
- Techspec: `GlobalExceptionHandler` mapeia 7 novas exceptions para ProblemDetails
- Techspec: `ApplicationServiceExtensions` e `ServiceCollectionExtensions` com registros completos
- `rules/restful.md`: Padrões REST com versionamento `api/v1/`, ProblemDetails RFC 7807
- `rules/dotnet-architecture.md`: Controller thin, delega para `IDispatcher`

## Subtarefas

### Request DTOs

- [x] 5.1 Criar `CreateBudgetRequest` em `1-Services/.../Controllers/Requests/CreateBudgetRequest.cs`:
  ```csharp
  public record CreateBudgetRequest(
      string Name,
      decimal Percentage,
      int ReferenceYear,
      int ReferenceMonth,
      List<Guid> CategoryIds,
      bool IsRecurrent = false
  );
  ```

- [x] 5.2 Criar `UpdateBudgetRequest` em `1-Services/.../Controllers/Requests/UpdateBudgetRequest.cs`:
  ```csharp
  public record UpdateBudgetRequest(
      string Name,
      decimal Percentage,
      List<Guid> CategoryIds,
      bool IsRecurrent
  );
  ```

### BudgetsController

- [x] 5.3 Criar `BudgetsController` em `1-Services/.../Controllers/BudgetsController.cs`:
  - Atributos: `[ApiController]`, `[Route("api/v1/budgets")]`, `[Authorize]`
  - Dependência: `IDispatcher`
  - Extrair `userId` via `User.GetUserId()` (ClaimsPrincipalExtensions)

  **Endpoints:**

  - `POST /api/v1/budgets` — Criar orçamento
    - Recebe `CreateBudgetRequest`
    - Cria `CreateBudgetCommand` e despacha via `IDispatcher`
    - Retorna `CreatedAtAction` (201) com `BudgetResponse`

  - `PUT /api/v1/budgets/{id}` — Editar orçamento
    - Recebe `UpdateBudgetRequest` + `Guid id`
    - Cria `UpdateBudgetCommand` e despacha
    - Retorna `Ok` (200) com `BudgetResponse`

  - `DELETE /api/v1/budgets/{id}` — Excluir orçamento
    - Recebe `Guid id`
    - Cria `DeleteBudgetCommand` e despacha
    - Retorna `NoContent` (204)

  - `GET /api/v1/budgets/{id}` — Obter por ID
    - Recebe `Guid id`
    - Cria `GetBudgetByIdQuery` e despacha
    - Retorna `Ok` (200) com `BudgetResponse`

  - `GET /api/v1/budgets?month=X&year=Y` — Listar por mês
    - Recebe `[FromQuery] int month, int year`
    - Cria `ListBudgetsQuery` e despacha
    - Retorna `Ok` (200) com `IReadOnlyList<BudgetResponse>`

  - `GET /api/v1/budgets/summary?month=X&year=Y` — Dashboard consolidado
    - Recebe `[FromQuery] int month, int year`
    - Cria `GetBudgetSummaryQuery` e despacha
    - Retorna `Ok` (200) com `BudgetSummaryResponse`

  - `GET /api/v1/budgets/available-percentage?month=X&year=Y&excludeBudgetId=Z` — Percentual disponível
    - Recebe `[FromQuery] int month, int year, Guid? excludeBudgetId`
    - Cria `GetAvailablePercentageQuery` e despacha
    - Retorna `Ok` (200) com `AvailablePercentageResponse`

### GlobalExceptionHandler

- [x] 5.4 Adicionar mapeamento das 7 novas exceptions no `GlobalExceptionHandler`:
  - Em `1-Services/.../Middleware/GlobalExceptionHandler.cs`
  - Mapear cada exception para ProblemDetails com status HTTP apropriado:

  | Exception | HTTP Status | Title |
  |-----------|-------------|-------|
  | `BudgetNotFoundException` | 404 Not Found | Orçamento não encontrado |
  | `BudgetPercentageExceededException` | 422 Unprocessable Entity | Percentual excede 100% |
  | `CategoryAlreadyBudgetedException` | 409 Conflict | Categoria já vinculada a orçamento |
  | `BudgetPeriodLockedException` | 422 Unprocessable Entity | Período bloqueado |
  | `BudgetMustHaveCategoriesException` | 422 Unprocessable Entity | Orçamento sem categorias |
  | `BudgetNameAlreadyExistsException` | 409 Conflict | Nome já existe |
  | `InvalidBudgetCategoryTypeException` | 422 Unprocessable Entity | Categoria inválida |

### Consolidação DI

- [x] 5.5 Verificar e consolidar todos os registros DI:
  - Em `ApplicationServiceExtensions`:
    - Handlers: `CreateBudgetCommandHandler`, `UpdateBudgetCommandHandler`, `DeleteBudgetCommandHandler`
    - Handlers: `ListBudgetsQueryHandler`, `GetBudgetByIdQueryHandler`, `GetBudgetSummaryQueryHandler`, `GetAvailablePercentageQueryHandler`
    - Validators: `CreateBudgetValidator`, `UpdateBudgetValidator`
    - Domain Service: `BudgetDomainService`
  - Em `ServiceCollectionExtensions` (Infra):
    - Repository: `IBudgetRepository` → `BudgetRepository`
  - Verificar que todos os registros são `Scoped` (padrão do projeto)

### Validação

- [x] 5.6 Testar endpoints manualmente com curl/Postman ou HTTP files:
  - Criar orçamento com dados válidos → 201
  - Criar com nome duplicado → 409
  - Criar com percentual > 100% restante → 422
  - Listar por mês → 200 com lista
  - Obter summary → 200 com consolidado
  - Editar orçamento → 200
  - Excluir orçamento → 204
  - Requisição sem token → 401

- [x] 5.7 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: 2.0 (Infra — repo real necessário), 3.0 (Commands), 4.0 (Queries)
- Desbloqueia: 8.0 (Testes Integração Backend), 9.0 (Frontend)
- Paralelizável: Não (depende de todas as camadas anteriores estarem prontas)

## Detalhes de Implementação

### Estrutura de Arquivos

```
backend/1-Services/GestorFinanceiro.Financeiro.API/
├── Controllers/
│   └── BudgetsController.cs                   ← NOVO
├── Controllers/Requests/
│   ├── CreateBudgetRequest.cs                  ← NOVO
│   └── UpdateBudgetRequest.cs                  ← NOVO
└── Middleware/
    └── GlobalExceptionHandler.cs               ← MODIFICAR (add exceptions)
```

### Padrões a Seguir

- Seguir padrão de `TransactionsController` para estrutura e convenções
- Seguir padrão de `AccountsController` para CRUD completo
- Controller deve ser thin — apenas mapear request → command/query e despachar
- Usar `[ProducesResponseType]` para documentar responses
- Seguir naming convention: `Get`, `Create`, `Update`, `Delete` nos nomes de action

### Nota sobre Rotas

O endpoint `GET /api/v1/budgets/summary` e `GET /api/v1/budgets/available-percentage` devem ser definidos ANTES de `GET /api/v1/budgets/{id}` para evitar conflito de roteamento (ASP.NET Core usa order-based matching). Usar `[HttpGet("summary")]` e `[HttpGet("available-percentage")]` com route constraints ou ordering adequado.

## Critérios de Sucesso

- 7 endpoints REST funcionais e acessíveis via HTTP
- Request DTOs mapeiam corretamente para commands/queries
- Todos os endpoints requerem autenticação JWT (401 sem token)
- `GlobalExceptionHandler` retorna ProblemDetails correto para cada exception
- Endpoints de criação retornam 201 Created com Location header
- Endpoint de exclusão retorna 204 No Content
- Endpoints de consulta retornam 200 com dados corretos
- Todos os registros DI completados sem conflitos
- Build compila sem erros
- API inicia sem erros em runtime
```
