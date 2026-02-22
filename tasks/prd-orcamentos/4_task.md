```markdown
---
status: completed
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>application/queries</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"5.0"</unblocks>
</task_context>

# Tarefa 4.0: Application Layer — Queries e DTOs de Response

## Visão Geral

Implementar as 4 queries da feature de Orçamentos e os DTOs de response: `ListBudgetsQuery` (listagem por mês), `GetBudgetByIdQuery` (detalhe individual), `GetBudgetSummaryQuery` (dashboard consolidado) e `GetAvailablePercentageQuery` (percentual disponível para formulário). Os DTOs incluem campos calculados (renda, limite, consumido, restante, percentual consumido). A query de summary é a mais complexa — agrega renda, consumido por orçamento e gastos fora de orçamento usando queries paralelas (`Task.WhenAll`).

## Requisitos

- PRD F2 req 14-21: Dashboard com cards, resumo consolidado, filtro de mês, gastos fora de orçamento
- PRD F3 req 22-28: Cálculo automático de saldo consumido a partir de transações
- PRD F4 req 30-32: Histórico de meses anteriores (somente leitura)
- Techspec: `BudgetResponse` com 14 campos (incluindo calculados)
- Techspec: `BudgetSummaryResponse` com consolidado + lista de budgets
- Techspec: `AvailablePercentageResponse` com percentual disponível e categorias em uso
- Techspec: `BudgetCategoryDto` com Id e Name
- Techspec: Endpoint `GET /summary` pode usar `Task.WhenAll` para queries paralelas
- `rules/dotnet-architecture.md`: CQRS com `IQuery<T>`, handlers via `IDispatcher`

## Subtarefas

### DTOs de Response

- [x] 4.1 Criar DTOs em `2-Application/GestorFinanceiro.Financeiro.Application/Dtos/`:
  - `BudgetResponse.cs`:
    ```csharp
    public record BudgetResponse(
        Guid Id,
        string Name,
        decimal Percentage,
        int ReferenceYear,
        int ReferenceMonth,
        bool IsRecurrent,
        decimal MonthlyIncome,
        decimal LimitAmount,
        decimal ConsumedAmount,
        decimal RemainingAmount,
        decimal ConsumedPercentage,
        IReadOnlyList<BudgetCategoryDto> Categories,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
    ```
  - `BudgetCategoryDto.cs`:
    ```csharp
    public record BudgetCategoryDto(Guid Id, string Name);
    ```
  - `BudgetSummaryResponse.cs`:
    ```csharp
    public record BudgetSummaryResponse(
        int ReferenceYear,
        int ReferenceMonth,
        decimal MonthlyIncome,
        decimal TotalBudgetedPercentage,
        decimal TotalBudgetedAmount,
        decimal TotalConsumedAmount,
        decimal TotalRemainingAmount,
        decimal UnbudgetedPercentage,
        decimal UnbudgetedAmount,
        decimal UnbudgetedExpenses,
        IReadOnlyList<BudgetResponse> Budgets
    );
    ```
  - `AvailablePercentageResponse.cs`:
    ```csharp
    public record AvailablePercentageResponse(
        decimal UsedPercentage,
        decimal AvailablePercentage,
        IReadOnlyList<Guid> UsedCategoryIds
    );
    ```

### ListBudgetsQuery

- [x] 4.2 Criar `ListBudgetsQuery` em `2-Application/.../Queries/Budget/ListBudgetsQuery.cs`:
  - Implementar `IQuery<IReadOnlyList<BudgetResponse>>`
  - Propriedades: `Year` (int), `Month` (int)

- [x] 4.3 Criar `ListBudgetsQueryHandler` em `2-Application/.../Queries/Budget/ListBudgetsQueryHandler.cs`:
  - Implementar `IQueryHandler<ListBudgetsQuery, IReadOnlyList<BudgetResponse>>`
  - Fluxo:
    1. Buscar budgets do mês via `IBudgetRepository.GetByMonthAsync()`
    2. Buscar renda mensal via `IBudgetRepository.GetMonthlyIncomeAsync()`
    3. Para cada budget, buscar consumido via `IBudgetRepository.GetConsumedAmountAsync()`
    4. Buscar nomes das categorias via `ICategoryRepository`
    5. Montar `BudgetResponse` com campos calculados:
       - `LimitAmount = monthlyIncome × (percentage / 100)`
       - `ConsumedPercentage = limitAmount > 0 ? (consumedAmount / limitAmount × 100) : 0`
       - `RemainingAmount = limitAmount - consumedAmount`

### GetBudgetByIdQuery

- [x] 4.4 Criar `GetBudgetByIdQuery` em `2-Application/.../Queries/Budget/GetBudgetByIdQuery.cs`:
  - Implementar `IQuery<BudgetResponse>`
  - Propriedade: `Id` (Guid)

- [x] 4.5 Criar `GetBudgetByIdQueryHandler`:
  - Buscar budget via `GetByIdWithCategoriesAsync()` → `BudgetNotFoundException`
  - Calcular renda, consumido e montar response (mesmo padrão do List)

### GetBudgetSummaryQuery

- [x] 4.6 Criar `GetBudgetSummaryQuery` em `2-Application/.../Queries/Budget/GetBudgetSummaryQuery.cs`:
  - Implementar `IQuery<BudgetSummaryResponse>`
  - Propriedades: `Year` (int), `Month` (int)

- [x] 4.7 Criar `GetBudgetSummaryQueryHandler`:
  - Fluxo otimizado com `Task.WhenAll`:
    1. Em paralelo: buscar budgets, renda mensal, gastos fora de orçamento
    2. Para cada budget: calcular consumido (pode usar múltiplas queries paralelas)
    3. Calcular consolidado:
       - `TotalBudgetedPercentage` = soma de todos os percentuais
       - `TotalBudgetedAmount` = soma de todos os limites
       - `TotalConsumedAmount` = soma de todos os consumidos
       - `TotalRemainingAmount` = `TotalBudgetedAmount - TotalConsumedAmount`
       - `UnbudgetedPercentage` = `100 - TotalBudgetedPercentage`
       - `UnbudgetedAmount` = `monthlyIncome × (UnbudgetedPercentage / 100)`
       - `UnbudgetedExpenses` = resultado de `GetUnbudgetedExpensesAsync()`
    4. Retornar `BudgetSummaryResponse`

### GetAvailablePercentageQuery

- [x] 4.8 Criar `GetAvailablePercentageQuery` em `2-Application/.../Queries/Budget/GetAvailablePercentageQuery.cs`:
  - Implementar `IQuery<AvailablePercentageResponse>`
  - Propriedades: `Year` (int), `Month` (int), `ExcludeBudgetId` (Guid?, optional)

- [x] 4.9 Criar `GetAvailablePercentageQueryHandler`:
  - Buscar percentual usado via `GetTotalPercentageForMonthAsync(excludeBudgetId)`
  - Buscar categoryIds em uso via `GetUsedCategoryIdsForMonthAsync(excludeBudgetId)`
  - Calcular `AvailablePercentage = 100 - UsedPercentage`
  - Retornar `AvailablePercentageResponse`

### Registro DI

- [x] 4.10 Registrar query handlers em `ApplicationServiceExtensions`:
  - `ListBudgetsQueryHandler`, `GetBudgetByIdQueryHandler`, `GetBudgetSummaryQueryHandler`, `GetAvailablePercentageQueryHandler`

### Testes Unitários

- [x] 4.11 Criar testes para `ListBudgetsQueryHandler` em `5-Tests/.../UnitTests/Application/Queries/Budget/ListBudgetsQueryHandlerTests.cs`:
  - `Handle_WithBudgetsInMonth_ShouldReturnListWithCalculatedFields`
  - `Handle_WithNoBudgets_ShouldReturnEmptyList`
  - `Handle_ShouldCalculateLimitCorrectly`
  - `Handle_ShouldCalculateConsumedPercentageCorrectly`
  - `Handle_WithZeroIncome_ShouldReturnZeroLimits`

- [x] 4.12 Criar testes para `GetBudgetByIdQueryHandler`:
  - `Handle_WithExistingBudget_ShouldReturnResponse`
  - `Handle_WithNonExistingBudget_ShouldThrowBudgetNotFoundException`

- [x] 4.13 Criar testes para `GetBudgetSummaryQueryHandler`:
  - `Handle_ShouldReturnConsolidatedSummary`
  - `Handle_ShouldCalculateTotalsCorrectly`
  - `Handle_ShouldIncludeUnbudgetedExpenses`
  - `Handle_WithNoBudgets_ShouldReturnEmptySummary`
  - `Handle_WithZeroIncome_ShouldReturnZeroAmounts`

- [x] 4.14 Criar testes para `GetAvailablePercentageQueryHandler`:
  - `Handle_WithSomeBudgets_ShouldReturnCorrectAvailable`
  - `Handle_WithNoBudgets_ShouldReturn100Available`
  - `Handle_WithExcludeBudgetId_ShouldExcludeFromCalculation`

### Validação

- [x] 4.15 Validar build e rodar testes unitários

## Sequenciamento

- Bloqueado por: 1.0 (Domain Layer — interfaces e entidade necessárias)
- Desbloqueia: 5.0 (API Layer — controller usa as queries)
- Paralelizável: Sim com 2.0 (Infra) e 3.0 (Commands) — queries usam mocks nos testes

## Detalhes de Implementação

### Estrutura de Arquivos

```
backend/2-Application/GestorFinanceiro.Financeiro.Application/
├── Dtos/
│   ├── BudgetResponse.cs                       ← NOVO
│   ├── BudgetCategoryDto.cs                    ← NOVO
│   ├── BudgetSummaryResponse.cs                ← NOVO
│   └── AvailablePercentageResponse.cs          ← NOVO
├── Queries/
│   └── Budget/
│       ├── ListBudgetsQuery.cs                 ← NOVO
│       ├── ListBudgetsQueryHandler.cs          ← NOVO
│       ├── GetBudgetByIdQuery.cs               ← NOVO
│       ├── GetBudgetByIdQueryHandler.cs        ← NOVO
│       ├── GetBudgetSummaryQuery.cs            ← NOVO
│       ├── GetBudgetSummaryQueryHandler.cs     ← NOVO
│       ├── GetAvailablePercentageQuery.cs      ← NOVO
│       └── GetAvailablePercentageQueryHandler.cs ← NOVO
└── Common/
    └── ApplicationServiceExtensions.cs         ← MODIFICAR (add registros)

backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/
└── Application/Queries/Budget/
    ├── ListBudgetsQueryHandlerTests.cs         ← NOVO
    ├── GetBudgetByIdQueryHandlerTests.cs       ← NOVO
    ├── GetBudgetSummaryQueryHandlerTests.cs    ← NOVO
    └── GetAvailablePercentageQueryHandlerTests.cs ← NOVO
```

### Lógica de Montagem do BudgetResponse (Helper)

Considerar criar um método helper privado `BuildBudgetResponse(Budget, decimal monthlyIncome, decimal consumedAmount, IReadOnlyList<BudgetCategoryDto> categories)` reutilizável entre ListBudgets, GetById e Summary para evitar duplicação de cálculos.

### Padrões a Seguir

- Seguir padrão de `GetDashboardSummaryQueryHandler` para queries agregadas
- Seguir padrão de `ListTransactionsQueryHandler` para listagem com filtros
- DTOs como `record` imutáveis (padrão existente)
- Usar `CancellationToken` em todas as operações async

## Critérios de Sucesso

- 4 DTOs de response criados como records imutáveis
- 4 queries + 4 handlers criados com padrão CQRS
- `GetBudgetSummaryQueryHandler` calcula consolidado corretamente (renda, orçado, consumido, restante, fora de orçamento)
- `GetAvailablePercentageQueryHandler` retorna percentual disponível e categorias em uso
- Campos calculados: `LimitAmount`, `ConsumedAmount`, `RemainingAmount`, `ConsumedPercentage` corretos
- `ConsumedPercentage` retorna 0 quando `LimitAmount` é 0 (evita divisão por zero)
- Handlers registrados no DI
- Todos os testes unitários passam
- Build compila sem erros
```
