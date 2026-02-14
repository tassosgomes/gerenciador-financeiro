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
<complexity>high</complexity>
<dependencies>http_server</dependencies>
<unblocks>"7.0", "9.0"</unblocks>
</task_context>

# Tarefa 6.0: Controller de Transações

## Visão Geral

Implementar o `TransactionsController` — o maior controller da API, com múltiplos verbos HTTP, filtros avançados e paginação offset-based. Suporta: criação de transações simples, parceladas, recorrentes e transferências; listagem com filtros e paginação; detalhe; ajuste; cancelamento individual e de grupo de parcelas.

Requer a criação de novas queries com suporte a filtros e paginação, pois as queries existentes da Fase 1 são simples (sem filtros). Os commands de escrita existentes são reutilizados (com adaptação para `UserId`).

## Requisitos

- Techspec F4: Todos os endpoints de transações
- PRD F4 req 20-30: CRUD de transações com filtros e paginação
- `rules/restful.md`: Paginação com `_page`/`_size`, filtros via query string
- Techspec: Paginação offset-based `_page` (default 1), `_size` (default 20, max 100)

## Subtarefas

### TransactionsController

- [ ] 6.1 Criar `TransactionsController` em `1-Services/Controllers/TransactionsController.cs`:
  - Route: `api/v1/transactions`
  - Atributos `[ApiController]`, `[Authorize]`
  - Injetar `IDispatcher`

### Endpoints de Criação

- [ ] 6.2 Endpoint `POST /api/v1/transactions` (transação simples):
  - Request body: `{ "accountId": "...", "categoryId": "...", "type": "...", "amount": 100.00, "description": "...", "competenceDate": "...", "dueDate": "...", "operationId": "..." }`
  - Extrair `userId` do JWT
  - Despachar `CreateTransactionCommand` existente
  - Retornar 201 Created com `TransactionResponse`
- [ ] 6.3 Endpoint `POST /api/v1/transactions/installments` (parcelada):
  - Request body adicional: `numberOfInstallments`
  - Despachar `CreateInstallmentCommand` existente
  - Retornar 201 Created com lista de `TransactionResponse` (parcelas geradas)
- [ ] 6.4 Endpoint `POST /api/v1/transactions/recurrences` (recorrente):
  - Request body: template de recorrência
  - Despachar `CreateRecurrenceCommand` existente
  - Retornar 201 Created com `RecurrenceTemplateResponse`
- [ ] 6.5 Endpoint `POST /api/v1/transactions/transfers` (transferência):
  - Request body: `{ "sourceAccountId": "...", "destinationAccountId": "...", "amount": 100.00, ... }`
  - Despachar `CreateTransferCommand` existente
  - Retornar 201 Created com par de `TransactionResponse`

### Endpoints de Leitura

- [ ] 6.6 Endpoint `GET /api/v1/transactions` (listar com filtros e paginação):
  - Query params: `accountId`, `categoryId`, `type`, `status`, `competenceDateFrom`, `competenceDateTo`, `dueDateFrom`, `dueDateTo`, `_page`, `_size`
  - Despachar nova `ListTransactionsQuery`
  - Retornar 200 com `PagedResult<TransactionResponse>`
- [ ] 6.7 Endpoint `GET /api/v1/transactions/{id}` (detalhe):
  - Despachar `GetTransactionByIdQuery` existente
  - Retornar 200 com `TransactionResponse` (incluindo dados de auditoria)

### Endpoints de Ação

- [ ] 6.8 Endpoint `POST /api/v1/transactions/{id}/adjustments` (ajuste):
  - Request body: `{ "newAmount": 150.00, "description": "...", "operationId": "..." }`
  - Extrair `userId` do JWT
  - Despachar `AdjustTransactionCommand` existente
  - Retornar 201 Created com `TransactionResponse` do ajuste
- [ ] 6.9 Endpoint `POST /api/v1/transactions/{id}/cancel` (cancelar):
  - Request body (opcional): `{ "reason": "...", "operationId": "..." }`
  - Extrair `userId` do JWT
  - Despachar `CancelTransactionCommand` existente
  - Retornar 200 com `TransactionResponse` atualizada
- [ ] 6.10 Endpoint `POST /api/v1/transactions/installment-groups/{groupId}/cancel` (cancelar grupo):
  - Request body (opcional): `{ "operationId": "..." }`
  - Extrair `userId` do JWT
  - Despachar `CancelInstallmentGroupCommand` existente
  - Retornar 200 com lista de `TransactionResponse` canceladas

### Novas Queries com Filtros e Paginação

- [ ] 6.11 Criar `ListTransactionsQuery` em `2-Application/Queries/Transaction/`:
  - Parâmetros: AccountId?, CategoryId?, Type?, Status?, CompetenceDateFrom?, CompetenceDateTo?, DueDateFrom?, DueDateTo?, Page, Size
  - Implementar handler com `IQueryable` filtering + paginação
  - Usar `AsNoTracking()` para performance
  - Retornar `PagedResult<TransactionResponse>`
- [ ] 6.12 Criar `ListTransactionsQueryValidator`:
  - `_page` ≥ 1
  - `_size` entre 1 e 100
  - Datas: `from` ≤ `to` quando ambas fornecidas
  - `type` e `status` devem ser valores válidos do enum

### Adaptações nos Commands Existentes

- [ ] 6.13 Adaptar commands de transação para receber `UserId`:
  - `CreateTransactionCommand` → adicionar campo `UserId`
  - `AdjustTransactionCommand` → adicionar campo `UserId`
  - `CancelTransactionCommand` → adicionar campo `UserId`
  - `CreateInstallmentCommand` → adicionar campo `UserId`
  - `CancelInstallmentGroupCommand` → adicionar campo `UserId`
  - `CreateRecurrenceCommand` → adicionar campo `UserId`
  - `CreateTransferCommand` → adicionar campo `UserId`
  - `CancelTransferCommand` → adicionar campo `UserId`
  - **Atualizar handlers** para usar userId na auditoria (`SetAuditOnCreate`/`SetAuditOnUpdate`)

### Request DTOs

- [ ] 6.14 Criar request DTOs:
  - `CreateTransactionRequest` (AccountId, CategoryId, Type, Amount, Description, CompetenceDate, DueDate, OperationId)
  - `CreateInstallmentRequest` (mesmos + NumberOfInstallments)
  - `CreateRecurrenceRequest` (AccountId, CategoryId, Type, Amount, Description, StartDate, EndDate?, DayOfMonth)
  - `CreateTransferRequest` (SourceAccountId, DestinationAccountId, Amount, Description, CompetenceDate, DueDate, OperationId)
  - `AdjustTransactionRequest` (NewAmount, Description, OperationId)
  - `CancelTransactionRequest` (Reason?, OperationId)
  - `CancelInstallmentGroupRequest` (OperationId)

### Testes Unitários

- [ ] 6.15 Testes para `ListTransactionsQueryHandler`:
  - Listagem sem filtros retorna paginado
  - Filtro por accountId retorna apenas transações da conta
  - Filtro por período de competência funciona
  - Filtro por status funciona
  - Paginação com page/size corretos
  - Page > total retorna lista vazia com metadados corretos
- [ ] 6.16 Testes para `ListTransactionsQueryValidator`:
  - Page 0 é inválido
  - Size > 100 é inválido
  - DateFrom > DateTo é inválido
- [ ] 6.17 Testes para `TransactionsController`:
  - Create retorna 201
  - List retorna 200 com metadados de paginação
  - GetById retorna 200
  - Adjust retorna 201
  - Cancel retorna 200

### Validação

- [ ] 6.18 Validar build com `dotnet build` a partir de `backend/`
- [ ] 6.19 Verificar que testes existentes da Fase 1 continuam passando

## Sequenciamento

- Bloqueado por: 3.0 (Pipeline HTTP, DI, JWT config)
- Desbloqueia: 7.0 (Auditoria — precisa dos handlers adaptados), 9.0 (Testes de Integração)
- Paralelizável: Sim (pode ser executada em paralelo com 4.0 e 5.0)

## Detalhes de Implementação

### TransactionsController — Listagem com filtros (exemplo)

```csharp
[HttpGet]
[ProducesResponseType(typeof(PagedResult<TransactionResponse>), StatusCodes.Status200OK)]
public async Task<IActionResult> List(
    [FromQuery] Guid? accountId,
    [FromQuery] Guid? categoryId,
    [FromQuery] TransactionType? type,
    [FromQuery] TransactionStatus? status,
    [FromQuery] DateTime? competenceDateFrom,
    [FromQuery] DateTime? competenceDateTo,
    [FromQuery] DateTime? dueDateFrom,
    [FromQuery] DateTime? dueDateTo,
    [FromQuery(Name = "_page")] int page = 1,
    [FromQuery(Name = "_size")] int size = 20,
    CancellationToken ct = default)
{
    var query = new ListTransactionsQuery(
        accountId, categoryId, type, status,
        competenceDateFrom, competenceDateTo,
        dueDateFrom, dueDateTo, page, size);
    var result = await _dispatcher.SendAsync(query, ct);
    return Ok(result);
}
```

### ListTransactionsQueryHandler — Paginação (exemplo)

```csharp
public async Task<PagedResult<TransactionResponse>> HandleAsync(
    ListTransactionsQuery query, CancellationToken ct)
{
    var queryable = _context.Transactions.AsNoTracking().AsQueryable();

    if (query.AccountId.HasValue)
        queryable = queryable.Where(t => t.AccountId == query.AccountId.Value);
    if (query.CategoryId.HasValue)
        queryable = queryable.Where(t => t.CategoryId == query.CategoryId.Value);
    if (query.Type.HasValue)
        queryable = queryable.Where(t => t.Type == query.Type.Value);
    if (query.Status.HasValue)
        queryable = queryable.Where(t => t.Status == query.Status.Value);
    if (query.CompetenceDateFrom.HasValue)
        queryable = queryable.Where(t => t.CompetenceDate >= query.CompetenceDateFrom.Value);
    if (query.CompetenceDateTo.HasValue)
        queryable = queryable.Where(t => t.CompetenceDate <= query.CompetenceDateTo.Value);
    // ... mais filtros

    var total = await queryable.CountAsync(ct);
    var items = await queryable
        .OrderByDescending(t => t.CompetenceDate)
        .Skip((query.Page - 1) * query.Size)
        .Take(query.Size)
        .ToListAsync(ct);

    var dtos = items.Adapt<List<TransactionResponse>>();
    var pagination = new PaginationMetadata(query.Page, query.Size, total,
        (int)Math.Ceiling((double)total / query.Size));

    return new PagedResult<TransactionResponse>(dtos, pagination);
}
```

### Formato da resposta paginada

```json
{
  "data": [
    { "id": "...", "amount": 100.00, "type": "Expense", ... }
  ],
  "pagination": {
    "page": 1,
    "size": 20,
    "total": 150,
    "totalPages": 8
  }
}
```

## Critérios de Sucesso

- Todos os endpoints de transação funcionais (criar, listar, detalhar, ajustar, cancelar)
- Tipos especiais: parcelamento, recorrência e transferência via endpoints dedicados
- Filtros múltiplos funcionando em combinação
- Paginação com metadados corretos (page, size, total, totalPages)
- `_page` e `_size` como nomes dos query params (conforme restful.md)
- Commands da Fase 1 adaptados para `UserId` sem quebrar funcionalidade
- Testes unitários da nova query de listagem passam
- Testes existentes continuam passando
- Build compila sem erros
```
