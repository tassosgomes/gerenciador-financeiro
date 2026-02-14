```markdown
---
status: pending
parallelizable: true
blocked_by: ["3.0"]
---

<task_context>
<domain>engine/serviços</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"7.0", "9.0"</unblocks>
</task_context>

# Tarefa 5.0: Controllers de Contas e Categorias

## Visão Geral

Implementar os controllers REST para contas (`AccountsController`) e categorias (`CategoriesController`). Estes controllers reutilizam os Commands/Queries da Fase 1 já existentes na Application Layer, adicionando apenas a camada HTTP com validação de entrada, autorização e documentação Swagger.

## Requisitos

- Techspec F2: Endpoints de contas (CRUD + ativar/desativar)
- Techspec F3: Endpoints de categorias (criar, listar, editar)
- PRD F2 req 10-15: CRUD de contas com filtros e validações
- PRD F3 req 16-19: CRUD de categorias com filtros
- `rules/restful.md`: Recursos em plural, filtros via query string
- `rules/dotnet-coding-standards.md`: Métodos ≤ 50 linhas

## Subtarefas

### AccountsController

- [ ] 5.1 Criar `AccountsController` em `1-Services/Controllers/AccountsController.cs`:
  - Route: `api/v1/accounts`
  - Atributos `[ApiController]`, `[Authorize]`
  - Injetar `IDispatcher`
- [ ] 5.2 Endpoint `POST /api/v1/accounts`:
  - Request body: `{ "name": "...", "type": "Checking|Savings|...", "initialBalance": 0.00, "allowNegativeBalance": false }`
  - Extrair `userId` do JWT (para auditoria `CreatedBy`)
  - Despachar `CreateAccountCommand` existente
  - Retornar 201 Created com `AccountResponse` e header `Location`
- [ ] 5.3 Endpoint `GET /api/v1/accounts`:
  - Query param opcional: `isActive` (bool)
  - Despachar `ListAccountsQuery` existente (adaptar se necessário para filtro)
  - Retornar 200 com lista de `AccountResponse`
- [ ] 5.4 Endpoint `GET /api/v1/accounts/{id}`:
  - Despachar `GetAccountByIdQuery` existente
  - Retornar 200 com `AccountResponse` (incluindo saldo atual)
- [ ] 5.5 Endpoint `PUT /api/v1/accounts/{id}`:
  - Request body: `{ "name": "...", "allowNegativeBalance": false }`
  - Extrair `userId` do JWT
  - Criar novo command `UpdateAccountCommand` (se não existir)
  - Retornar 200 com `AccountResponse` atualizado
- [ ] 5.6 Endpoint `PATCH /api/v1/accounts/{id}/status`:
  - Request body: `{ "isActive": true|false }`
  - Extrair `userId` do JWT
  - Despachar `ActivateAccountCommand` ou `DeactivateAccountCommand` existentes conforme flag
  - Retornar 204 No Content

### CategoriesController

- [ ] 5.7 Criar `CategoriesController` em `1-Services/Controllers/CategoriesController.cs`:
  - Route: `api/v1/categories`
  - Atributos `[ApiController]`, `[Authorize]`
  - Injetar `IDispatcher`
- [ ] 5.8 Endpoint `POST /api/v1/categories`:
  - Request body: `{ "name": "...", "type": "Income|Expense" }`
  - Extrair `userId` do JWT
  - Despachar `CreateCategoryCommand` existente
  - Retornar 201 Created com `CategoryResponse` e header `Location`
- [ ] 5.9 Endpoint `GET /api/v1/categories`:
  - Query param opcional: `type` (Income|Expense)
  - Despachar `ListCategoriesQuery` existente (adaptar para filtro por tipo se necessário)
  - Retornar 200 com lista de `CategoryResponse`
- [ ] 5.10 Endpoint `PUT /api/v1/categories/{id}`:
  - Request body: `{ "name": "..." }`
  - Extrair `userId` do JWT
  - Despachar `UpdateCategoryCommand` existente
  - Retornar 200 com `CategoryResponse` atualizado

### Adaptações nos Commands/Queries Existentes

- [ ] 5.11 Verificar e adaptar commands existentes para receber `UserId` (para `CreatedBy`/`UpdatedBy`):
  - `CreateAccountCommand` → adicionar campo `UserId` se não tiver
  - `DeactivateAccountCommand` → adicionar campo `UserId`
  - `ActivateAccountCommand` → adicionar campo `UserId`
  - `CreateCategoryCommand` → adicionar campo `UserId`
  - `UpdateCategoryCommand` → adicionar campo `UserId`
  - **Atenção**: atualizar os handlers correspondentes para usar userId na auditoria
- [ ] 5.12 Adaptar queries existentes para suportar filtros:
  - `ListAccountsQuery` → adicionar filtro `IsActive?`
  - `ListCategoriesQuery` → adicionar filtro `Type?`
- [ ] 5.13 Criar `UpdateAccountCommand` + `UpdateAccountCommandHandler` se não existir:
  - Input: AccountId, Name, AllowNegativeBalance, UserId
  - Buscar conta, atualizar campos permitidos, persistir

### Request DTOs

- [ ] 5.14 Criar request DTOs:
  - `CreateAccountRequest` (Name, Type, InitialBalance, AllowNegativeBalance)
  - `UpdateAccountRequest` (Name, AllowNegativeBalance)
  - `UpdateAccountStatusRequest` (IsActive)
  - `CreateCategoryRequest` (Name, Type)
  - `UpdateCategoryRequest` (Name)

### Testes Unitários

- [ ] 5.15 Testes para `AccountsController`:
  - Create retorna 201 com Location
  - List retorna 200 com filtro isActive
  - GetById retorna 200
  - Update retorna 200
  - UpdateStatus retorna 204
- [ ] 5.16 Testes para `CategoriesController`:
  - Create retorna 201 com Location
  - List retorna 200 com filtro type
  - Update retorna 200
- [ ] 5.17 Testes unitários para novos/adaptados commands (ex: `UpdateAccountCommand`)

### Validação

- [ ] 5.18 Validar build com `dotnet build` a partir de `backend/`
- [ ] 5.19 Verificar que testes existentes da Fase 1 continuam passando após adaptações

## Sequenciamento

- Bloqueado por: 3.0 (Pipeline HTTP, DI, JWT config)
- Desbloqueia: 7.0 (Auditoria), 9.0 (Testes de Integração)
- Paralelizável: Sim (pode ser executada em paralelo com 4.0, 6.0 e 8.0 — desde que 3.0 esteja completa)

## Detalhes de Implementação

### AccountsController (exemplo)

```csharp
[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AccountsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var command = new CreateAccountCommand(
            request.Name, request.Type, request.InitialBalance,
            request.AllowNegativeBalance, userId);
        var result = await _dispatcher.SendAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var query = new ListAccountsQuery(isActive);
        var result = await _dispatcher.SendAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetAccountByIdQuery(id);
        var result = await _dispatcher.SendAsync(query, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAccountRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var command = new UpdateAccountCommand(id, request.Name,
            request.AllowNegativeBalance, userId);
        var result = await _dispatcher.SendAsync(command, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateAccountStatusRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (request.IsActive)
            await _dispatcher.SendAsync(new ActivateAccountCommand(id, userId), ct);
        else
            await _dispatcher.SendAsync(new DeactivateAccountCommand(id, userId), ct);
        return NoContent();
    }
}
```

### Compatibilidade com Fase 1

Os commands da Fase 1 usam `CreatedBy` como string (ex: `"system"`). Nesta fase, passamos a usar `userId.ToString()` do JWT. Os dados existentes mantêm o valor antigo — compatibilidade garantida pois `CreatedBy` é `string`.

## Critérios de Sucesso

- CRUD completo de contas funcional via API
- CRUD de categorias funcional via API
- Filtros por `isActive` e `type` funcionando
- Auditoria (`CreatedBy`/`UpdatedBy`) preenchida com `userId` do JWT
- Todos os endpoints protegidos por `[Authorize]`
- Status codes corretos (200, 201, 204)
- Commands existentes da Fase 1 adaptados sem quebrar testes existentes
- Build compila sem erros
- Testes existentes continuam passando
```
