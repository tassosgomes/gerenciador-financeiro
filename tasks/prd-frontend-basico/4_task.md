---
status: pending
parallelizable: true
blocked_by: []
---

<task_context>
<domain>backend/infra</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>dotnet, entity_framework</dependencies>
<unblocks>"5.0", "6.0", "8.0"</unblocks>
</task_context>

# Tarefa 4.0: Ajustes no Backend para Suporte ao Frontend

## Visão Geral

Realizar os ajustes necessários no backend .NET (Fase 2) para que o frontend possa se integrar corretamente. Inclui: configuração de CORS, correção do DTO `AccountResponse` (campos faltantes), adição de suporte a filtros nas queries de listagem existentes, e garantir que a API responde no formato esperado pelo frontend. Esta tarefa pode ser executada em paralelo com as tarefas de frontend (1.0–3.0).

## Requisitos

- CORS configurado para permitir `http://localhost:5173` (dev) e origens configuráveis (prod)
- `AccountResponse` deve incluir `Type` (AccountType) e `AllowNegativeBalance` (bool) — campos faltantes no DTO atual
- `ListTransactionsQuery` deve suportar filtros: accountId, categoryId, type, status, competenceDateFrom, competenceDateTo
- `ListTransactionsQuery` deve suportar paginação (page, size)
- `ListAccountsQuery` deve suportar filtro opcional por status (ativa/inativa) e tipo
- `ListCategoriesQuery` deve suportar filtro opcional por tipo (Receita/Despesa)
- Validar que build e testes existentes continuam passando após alterações

## Subtarefas

- [ ] 4.1 Configurar CORS no `Program.cs` — permitir origens configuráveis via `appsettings.json` (default: `http://localhost:5173`), métodos GET/POST/PUT/PATCH/DELETE, headers Authorization e Content-Type
- [ ] 4.2 Corrigir `AccountResponse` DTO — adicionar propriedades `Type` (AccountType enum) e `AllowNegativeBalance` (bool); atualizar o mapping (AutoMapper/manual) do entity para o DTO
- [ ] 4.3 Evoluir `ListTransactionsQuery` (ou criar `ListTransactionsQuery` se não existir) — adicionar parâmetros opcionais: `AccountId?`, `CategoryId?`, `Type?`, `Status?`, `CompetenceDateFrom?`, `CompetenceDateTo?`, `Page` (default 1), `Size` (default 20)
- [ ] 4.4 Implementar handler da query de transações com filtros — aplicar filtros via LINQ `Where()` condicionais e paginação `Skip/Take`; retornar `PagedResponse<TransactionResponse>`
- [ ] 4.5 Adicionar filtro opcional de status (`IsActive?`) em `ListAccountsQuery` e seu handler
- [ ] 4.6 Adicionar filtro opcional de tipo (`Type?`: Receita/Despesa) em `ListCategoriesQuery` e seu handler
- [ ] 4.7 Adicionar configuração CORS em `appsettings.Development.json`: `"Cors": { "AllowedOrigins": ["http://localhost:5173"] }`
- [ ] 4.8 Validar build: `dotnet build` sem erros
- [ ] 4.9 Validar testes existentes: `dotnet test` sem regressões
- [ ] 4.10 Testar manualmente (ou via integration test) que CORS headers estão presentes nas respostas

## Sequenciamento

- Bloqueado por: Nenhum (pode começar imediatamente)
- Desbloqueia: 5.0 (Dashboard — parcialmente), 6.0 (Contas — DTO corrigido), 8.0 (Transações — filtros e paginação)
- Paralelizável: Sim, com 1.0, 2.0 e 3.0 (tarefas de frontend)

## Detalhes de Implementação

### CORS Configuration (`Program.cs`)

```csharp
// Em Program.cs, antes de app.Build()
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Após app.Build(), antes de app.MapControllers()
app.UseCors();
```

### AccountResponse DTO — Campos Faltantes

```csharp
// Adicionar ao DTO existente
public class AccountResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }           // NOVO
    public decimal Balance { get; set; }
    public bool AllowNegativeBalance { get; set; }  // NOVO
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### ListTransactionsQuery com Filtros

```csharp
public class ListTransactionsQuery : IRequest<PagedResponse<TransactionResponse>>
{
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public TransactionType? Type { get; set; }
    public TransactionStatus? Status { get; set; }
    public DateTime? CompetenceDateFrom { get; set; }
    public DateTime? CompetenceDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
```

### Handler com Filtros Condicionais

```csharp
public async Task<PagedResponse<TransactionResponse>> Handle(
    ListTransactionsQuery request, CancellationToken ct)
{
    var query = _context.Transactions.AsNoTracking();

    if (request.AccountId.HasValue)
        query = query.Where(t => t.AccountId == request.AccountId.Value);
    if (request.CategoryId.HasValue)
        query = query.Where(t => t.CategoryId == request.CategoryId.Value);
    if (request.Type.HasValue)
        query = query.Where(t => t.Type == request.Type.Value);
    if (request.Status.HasValue)
        query = query.Where(t => t.Status == request.Status.Value);
    if (request.CompetenceDateFrom.HasValue)
        query = query.Where(t => t.CompetenceDate >= request.CompetenceDateFrom.Value);
    if (request.CompetenceDateTo.HasValue)
        query = query.Where(t => t.CompetenceDate <= request.CompetenceDateTo.Value);

    var total = await query.CountAsync(ct);
    var items = await query
        .OrderByDescending(t => t.CompetenceDate)
        .Skip((request.Page - 1) * request.Size)
        .Take(request.Size)
        .Select(t => t.ToResponse())
        .ToListAsync(ct);

    return new PagedResponse<TransactionResponse>(items, request.Page, request.Size, total);
}
```

## Critérios de Sucesso

- `dotnet build` compila sem erros após alterações
- `dotnet test` — todos os testes existentes passam (sem regressões)
- Requisição do frontend (`http://localhost:5173`) para API recebe headers CORS corretos
- `GET /api/v1/accounts` retorna objetos com campos `type` e `allowNegativeBalance`
- `GET /api/v1/transactions?accountId=X&status=1&page=2&size=10` retorna dados filtrados e paginados
- `GET /api/v1/categories?type=1` retorna apenas categorias do tipo especificado
- Respostas paginadas incluem metadados: `page`, `size`, `total`, `totalPages`
