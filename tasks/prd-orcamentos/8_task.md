```markdown
---
status: done
parallelizable: false
blocked_by: ["5.0", "6.0", "7.0"]
---

<task_context>
<domain>testing/integração</domain>
<type>testing</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database</dependencies>
<unblocks>nenhuma</unblocks>
</task_context>

# Tarefa 8.0: Testes de Integração Backend (Repository + HTTP)

## Visão Geral

Implementar testes de integração para a feature de Orçamentos em duas frentes: (1) testes de repositório com Testcontainers + PostgreSQL real, validando queries agregadas, constraints de unicidade e operações CRUD; (2) testes HTTP com `WebApplicationFactory`, validando endpoints da API, autenticação, validação de input e responses ProblemDetails. Estes testes garantem que todos os componentes funcionam corretamente integrados com infraestrutura real.

## Requisitos

- Techspec "Testes de Integração" — Repository (Testcontainers) e HTTP (WebApplicationFactory)
- Techspec: Queries agregadas de renda, consumido e gastos fora de orçamento
- Techspec: Constraint UNIQUE `(category_id, reference_year, reference_month)` testada
- Techspec: CRUD completo via HTTP
- `rules/dotnet-testing.md`: Testcontainers para testes de integração, WebApplicationFactory para HTTP

## Subtarefas

### Testes de Repository (Testcontainers)

- [x] 8.1 Criar `BudgetRepositoryTests` em `5-Tests/.../IntegrationTests/Repository/BudgetRepositoryTests.cs`:
  - Usar Testcontainers PostgreSQL (seguir padrão existente de `IntegrationTests`)
  - Setup: criar banco limpo, aplicar migrations, seed de dados de teste

  **Cenários de teste:**

  - `GetByMonthAsync_ShouldReturnOnlyBudgetsOfSpecifiedMonth`
  - `GetByMonthAsync_WithNoBudgets_ShouldReturnEmptyList`
  - `GetByIdWithCategoriesAsync_ShouldReturnBudgetWithCategories`
  - `GetByIdWithCategoriesAsync_WithInvalidId_ShouldReturnNull`
  - `GetTotalPercentageForMonthAsync_ShouldReturnCorrectSum`
  - `GetTotalPercentageForMonthAsync_WithExcludeBudgetId_ShouldExclude`
  - `GetTotalPercentageForMonthAsync_WithNoBudgets_ShouldReturnZero`
  - `IsCategoryUsedInMonthAsync_WhenUsed_ShouldReturnTrue`
  - `IsCategoryUsedInMonthAsync_WhenNotUsed_ShouldReturnFalse`
  - `IsCategoryUsedInMonthAsync_WithExcludeBudgetId_ShouldExclude`
  - `GetUsedCategoryIdsForMonthAsync_ShouldReturnAllUsedIds`
  - `GetMonthlyIncomeAsync_ShouldSumOnlyCreditPaidTransactions`
  - `GetMonthlyIncomeAsync_ShouldExcludeCancelledTransactions`
  - `GetMonthlyIncomeAsync_WithNoTransactions_ShouldReturnZero`
  - `GetConsumedAmountAsync_ShouldSumOnlyDebitPaidOfSpecifiedCategories`
  - `GetConsumedAmountAsync_ShouldExcludeCancelledTransactions`
  - `GetConsumedAmountAsync_ShouldFilterByCompetenceMonth`
  - `GetConsumedAmountAsync_WithNoTransactions_ShouldReturnZero`
  - `GetUnbudgetedExpensesAsync_ShouldSumDebitPaidNotInAnyBudget`
  - `GetUnbudgetedExpensesAsync_WithAllCategoriesBudgeted_ShouldReturnZero`
  - `GetRecurrentBudgetsForMonthAsync_ShouldReturnOnlyRecurrent`
  - `ExistsByNameAsync_WhenExists_ShouldReturnTrue`
  - `ExistsByNameAsync_WhenNotExists_ShouldReturnFalse`
  - `ExistsByNameAsync_WithExcludeBudgetId_ShouldExclude`
  - `RemoveCategoryFromBudgetsAsync_ShouldRemoveFromAllBudgets`

- [x] 8.2 Testar constraints de unicidade:
  - `Insert_DuplicateCategoryInSameMonth_ShouldThrowException` — violação de UNIQUE `(category_id, reference_year, reference_month)`
  - `Insert_SameCategoryDifferentMonth_ShouldSucceed`
  - `Insert_DuplicateName_ShouldThrowException` — violação de UNIQUE `(name)`

- [x] 8.3 Testar CASCADE deletes:
  - `DeleteBudget_ShouldCascadeDeleteBudgetCategories`
  - `DeleteCategory_ShouldCascadeDeleteFromBudgetCategories`

### Testes HTTP Integration (WebApplicationFactory)

- [x] 8.4 Criar `BudgetsControllerTests` em `5-Tests/.../HttpIntegrationTests/Controllers/BudgetsControllerTests.cs`:
  - Usar `WebApplicationFactory` com banco real (Testcontainers)
  - Setup: autenticar usuário de teste, obter JWT token

  **Cenários CRUD:**

  - `PostBudget_WithValidData_ShouldReturn201WithBudgetResponse`
  - `PostBudget_WithInvalidData_ShouldReturn400WithValidationErrors`
  - `PostBudget_WithDuplicateName_ShouldReturn409`
  - `PostBudget_WithPercentageExceeding100_ShouldReturn422`
  - `PostBudget_WithCategoryAlreadyBudgeted_ShouldReturn409`
  - `PostBudget_WithPastMonth_ShouldReturn422`
  - `PostBudget_WithoutAuth_ShouldReturn401`
  - `PutBudget_WithValidData_ShouldReturn200`
  - `PutBudget_WithNonExistingId_ShouldReturn404`
  - `PutBudget_WithPastMonth_ShouldReturn422`
  - `DeleteBudget_WithValidId_ShouldReturn204`
  - `DeleteBudget_WithNonExistingId_ShouldReturn404`
  - `DeleteBudget_WithPastMonth_ShouldReturn422`

  **Cenários de Consulta:**

  - `GetBudgetById_WithValidId_ShouldReturn200WithResponse`
  - `GetBudgetById_WithInvalidId_ShouldReturn404`
  - `GetBudgets_WithMonthFilter_ShouldReturnFilteredList`
  - `GetBudgets_WithNoResults_ShouldReturn200EmptyList`
  - `GetBudgetSummary_ShouldReturn200WithConsolidatedData`
  - `GetBudgetSummary_ShouldIncludeCalculatedFields`
  - `GetAvailablePercentage_ShouldReturn200WithCorrectPercentage`
  - `GetAvailablePercentage_WithExcludeBudgetId_ShouldExclude`

### Dados de Teste

- [x] 8.5 Criar helpers de seed para testes:
  - Método para criar categorias de despesa de teste
  - Método para criar transações de teste (Credit/Debit com diferentes status)
  - Método para criar orçamentos de teste com categorias

### Validação

- [x] 8.6 Rodar todos os testes de integração:
  - `dotnet test --filter "IntegrationTests"` (requer Docker)
  - `dotnet test --filter "HttpIntegrationTests"` (requer Docker)
  - Verificar que todos passam sem falhas intermitentes

## Sequenciamento

- Bloqueado por: 5.0 (API Layer), 6.0 (Category Impact), 7.0 (RecurrenceWorker)
- Desbloqueia: Nenhum (fase final de validação backend)
- Paralelizável: Sim com 9.0, 10.0, 11.0 (Frontend é independente dos testes backend)

## Detalhes de Implementação

### Estrutura de Arquivos

```
backend/5-Tests/GestorFinanceiro.Financeiro.IntegrationTests/
└── Repository/
    └── BudgetRepositoryTests.cs                    ← NOVO

backend/5-Tests/GestorFinanceiro.Financeiro.HttpIntegrationTests/
└── Controllers/
    └── BudgetsControllerTests.cs                   ← NOVO
```

### Padrões a Seguir

- Seguir padrão de `TransactionRepositoryTests` para testes de repository
- Seguir padrão de `AccountsControllerTests` para testes HTTP
- Usar `IClassFixture<T>` para compartilhar container PostgreSQL entre testes
- Usar `[Collection]` se necessário para isolation
- Seed de dados no construtor ou método de setup
- Cleanup entre testes para evitar interferência

### Dados de Teste Necessários

Para testar queries agregadas, criar setup com:
- 2+ categorias de despesa
- 2+ categorias de receita
- Transações Credit/Paid (renda)
- Transações Debit/Paid nas categorias do orçamento (consumido)
- Transações Debit/Paid em categorias fora do orçamento (gastos fora)
- Transações Debit/Cancelled (devem ser ignoradas)
- 2+ orçamentos no mesmo mês com categorias diferentes

## Critérios de Sucesso

- Todos os testes de repository passam contra PostgreSQL real (Testcontainers)
- Queries de renda e consumido retornam valores corretos com dados reais
- Constraints de unicidade são verificadas no banco real
- CASCADE deletes funcionam corretamente
- Todos os endpoints da API retornam status codes corretos
- ProblemDetails são retornados para erros de domínio
- Autenticação JWT é obrigatória em todos os endpoints
- Validação FluentValidation funciona end-to-end
- Nenhuma regressão em testes existentes
```
