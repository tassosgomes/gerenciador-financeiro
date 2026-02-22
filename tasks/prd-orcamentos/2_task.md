```markdown
---
status: done
parallelizable: false
blocked_by: ["1.0"]
---

<task_context>
<domain>infra/persistência</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database</dependencies>
<unblocks>"5.0", "6.0", "7.0", "8.0"</unblocks>
</task_context>

# Tarefa 2.0: Infra Layer — Migration EF Core, BudgetConfiguration e BudgetRepository

## Visão Geral

Implementar toda a camada de infraestrutura de persistência para a feature de Orçamentos: configuração EF Core (Fluent API) para as tabelas `budgets` e `budget_categories`, implementação concreta do `BudgetRepository` com todas as queries agregadas (renda, consumido, gastos fora de orçamento), migration EF Core, e registro da `DbSet<Budget>` no `FinanceiroDbContext`. Esta tarefa é crítica pois habilita tanto a API (via repositório) quanto os testes de integração.

## Requisitos

- Techspec: Tabela `budgets` com colunas id, name, percentage, reference_year, reference_month, is_recurrent, created_by, created_at, updated_by, updated_at
- Techspec: Tabela `budget_categories` com desnormalização (budget_id, category_id, reference_year, reference_month)
- Techspec: Constraint `UNIQUE(category_id, reference_year, reference_month)` na tabela `budget_categories`
- Techspec: Constraint `UNIQUE(name)` na tabela `budgets`
- Techspec: Índice `ix_budgets_reference` em `(reference_year, reference_month)`
- Techspec: `BudgetRepository` implementando `IBudgetRepository` com queries otimizadas
- Techspec: Queries de renda e consumido com `AsNoTracking()` e projeções otimizadas
- PRD F3 req 23: Saldo consumido = soma Debit + Paid das categorias vinculadas no mês
- PRD F3 req 2: Renda mensal = soma Credit + Paid do mês (CompetenceDate)
- `rules/dotnet-architecture.md`: Infra implementa interfaces do Domain

## Subtarefas

### FinanceiroDbContext

- [x] 2.1 Adicionar `DbSet<Budget> Budgets` no `FinanceiroDbContext`:
  - Em `4-Infra/GestorFinanceiro.Financeiro.Infra/Context/FinanceiroDbContext.cs`
  - Registrar configuração no `OnModelCreating`

### BudgetConfiguration (Fluent API)

- [x] 2.2 Criar `BudgetConfiguration` em `4-Infra/GestorFinanceiro.Financeiro.Infra/Config/BudgetConfiguration.cs`:
  - Implementar `IEntityTypeConfiguration<Budget>`
  - Tabela `budgets`:
    - PK: `id` (uuid)
    - `name`: `varchar(150)`, NOT NULL, UNIQUE
    - `percentage`: `numeric(5,2)`, NOT NULL, CHECK > 0 AND ≤ 100
    - `reference_year`: `smallint`, NOT NULL
    - `reference_month`: `smallint`, NOT NULL, CHECK 1-12
    - `is_recurrent`: `boolean`, NOT NULL, DEFAULT false
    - `created_by`: `varchar(100)`, NOT NULL
    - `created_at`: `timestamptz`, NOT NULL
    - `updated_by`: `varchar(100)`, NULL
    - `updated_at`: `timestamptz`, NULL
  - Índice `ix_budgets_reference` em `(reference_year, reference_month)`
  - Mapear propriedade `CategoryIds` via tabela associativa `budget_categories`
  - Tabela `budget_categories`:
    - PK composta: `(budget_id, category_id)`
    - FK `budget_id` → `budgets.id` ON DELETE CASCADE
    - FK `category_id` → `categories.id` ON DELETE CASCADE
    - `reference_year`: `smallint`, NOT NULL
    - `reference_month`: `smallint`, NOT NULL
    - Constraint UNIQUE: `(category_id, reference_year, reference_month)`
  - Ignorar navegação inversa de `Category` para `Budget` (sem alterar entity Category)

### BudgetRepository

- [x] 2.3 Criar `BudgetRepository` em `4-Infra/GestorFinanceiro.Financeiro.Infra/Repository/BudgetRepository.cs`:
  - Herdar de `Repository<Budget>` (base existente)
  - Implementar `IBudgetRepository` com todos os 12 métodos:

  **Queries de Budget:**
  - `GetByMonthAsync`: Filtrar budgets por `ReferenceYear` e `ReferenceMonth`, incluir category IDs
  - `GetByIdWithCategoriesAsync`: Buscar budget por ID com category IDs carregados
  - `GetRecurrentBudgetsForMonthAsync`: Filtrar budgets com `IsRecurrent = true` no mês
  - `ExistsByNameAsync`: Verificar existência por nome (com `excludeBudgetId` opcional)

  **Queries de Percentual:**
  - `GetTotalPercentageForMonthAsync`: Somar `Percentage` de todos os budgets do mês (excluindo `excludeBudgetId`)
  - `IsCategoryUsedInMonthAsync`: Verificar se `categoryId` já está em `budget_categories` do mês (excluindo `excludeBudgetId`)
  - `GetUsedCategoryIdsForMonthAsync`: Listar todos os categoryIds em uso no mês (excluindo `excludeBudgetId`)

  **Queries de Transação (Agregadas):**
  - `GetMonthlyIncomeAsync`: `SUM(Amount)` WHERE `Type == Credit AND Status == Paid AND CompetenceDate` no mês — usar `AsNoTracking()`
  - `GetConsumedAmountAsync`: `SUM(Amount)` WHERE `Type == Debit AND Status == Paid AND CategoryId IN (categoryIds) AND CompetenceDate` no mês — usar `AsNoTracking()`
  - `GetUnbudgetedExpensesAsync`: `SUM(Amount)` WHERE `Type == Debit AND Status == Paid AND CategoryId NOT IN (all budgeted categoryIds) AND CompetenceDate` no mês — usar `AsNoTracking()`

  **Operações:**
  - `RemoveCategoryFromBudgetsAsync`: Executar SQL direto `DELETE FROM budget_categories WHERE category_id = @categoryId`
  - `Remove(Budget budget)`: Remover via DbSet

### Migration EF Core

- [x] 2.4 Gerar migration EF Core:
  - Executar `dotnet ef migrations add AddBudgets` no projeto Infra
  - Verificar que a migration cria ambas as tabelas com constraints corretas
  - Verificar índices e foreign keys

### Registro no DI

- [x] 2.5 Registrar `IBudgetRepository` → `BudgetRepository` em `ServiceCollectionExtensions.AddInfrastructure()`:
  - Em `4-Infra/GestorFinanceiro.Financeiro.Infra/DependencyInjection/ServiceCollectionExtensions.cs`
  - Adicionar `services.AddScoped<IBudgetRepository, BudgetRepository>()`

### Validação

- [x] 2.6 Validar migration aplicando em banco de dev local:
  - `dotnet ef database update` deve aplicar sem erros
  - Verificar schema das tabelas no PostgreSQL

- [x] 2.7 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: 1.0 (Domain Layer — entidade e interface necessárias)
- Desbloqueia: 5.0 (API Layer), 6.0 (Category Impact), 7.0 (RecurrenceWorker), 8.0 (Testes Integração)
- Paralelizável: Sim com 3.0 e 4.0 (Application layer usa mocks, não precisa da infra real)

## Detalhes de Implementação

### Estrutura de Arquivos

```
backend/4-Infra/GestorFinanceiro.Financeiro.Infra/
├── Config/
│   └── BudgetConfiguration.cs                     ← NOVO
├── Context/
│   └── FinanceiroDbContext.cs                      ← MODIFICAR (add DbSet)
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs              ← MODIFICAR (add repo)
├── Migrations/
│   └── XXXXXXXX_AddBudgets.cs                     ← NOVO (auto-gerado)
└── Repository/
    └── BudgetRepository.cs                         ← NOVO
```

### Mapeamento da Tabela Associativa

A tabela `budget_categories` precisa de atenção especial no EF Core pois contém colunas desnormalizadas (`reference_year`, `reference_month`) que devem ser sincronizadas com o budget pai. A abordagem recomendada é:

1. Criar uma entity intermediária `BudgetCategory` para mapeamento EF Core (não exposta no Domain)
2. Ou usar raw SQL para inserção e mapeamento via shadow properties

A opção mais simples e consistente com o projeto é usar shadow properties ou uma entidade de mapeamento interna ao Infra.

### Queries de Transação

As queries de `GetMonthlyIncomeAsync`, `GetConsumedAmountAsync` e `GetUnbudgetedExpensesAsync` devem:
- Usar `_context.Set<Transaction>()` diretamente (o BudgetRepository tem acesso ao DbContext)
- Aplicar filtros de `CompetenceDate.Year == year && CompetenceDate.Month == month`
- Filtrar `Status == TransactionStatus.Paid`
- Usar `AsNoTracking()` para performance
- Retornar 0 quando não houver transações (não null)

### Padrões a Seguir

- Seguir padrão de `AccountConfiguration.cs` para Fluent API
- Seguir padrão de `TransactionRepository.cs` para queries agregadas
- Seguir padrão de `DashboardRepository` para cálculo de renda mensal
- Registrar no DI seguindo padrão existente em `ServiceCollectionExtensions`

## Critérios de Sucesso

- Migration EF Core gera tabelas `budgets` e `budget_categories` corretamente
- Constraint UNIQUE `(name)` em `budgets` funciona
- Constraint UNIQUE `(category_id, reference_year, reference_month)` em `budget_categories` funciona
- FK com ON DELETE CASCADE funciona em ambas as tabelas
- `BudgetRepository` implementa todos os 12 métodos de `IBudgetRepository`
- Queries de renda e consumido retornam valores corretos com `AsNoTracking()`
- `GetUnbudgetedExpensesAsync` exclui corretamente categorias já orçadas
- Registro no DI funciona sem erros na inicialização
- Build do backend compila sem erros
- Migration aplica sem erros no banco de dev
```
