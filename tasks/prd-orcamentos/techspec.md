# Especificação Técnica — Orçamentos

## Resumo Executivo

A feature de Orçamentos introduz uma nova entidade de domínio `Budget` com relacionamento muitos-para-muitos com `Category` (através de uma tabela associativa `BudgetCategories`), seguindo a arquitetura Clean Architecture em 4 camadas já consolidada no projeto. Todos os valores monetários (renda mensal, valor limite, saldo consumido) são **calculados em tempo de consulta** a partir de transações existentes — não há materialização de saldos. O backend expõe endpoints REST sob `api/v1/budgets` com CQRS simplificado (Commands + Queries via `IDispatcher`). O frontend adiciona o módulo `features/budgets/` com dashboard de cards, formulário de criação/edição e filtro por mês/ano. Um novo `BudgetRecurrenceWorker` (BackgroundService) replica orçamentos recorrentes mensalmente de forma lazy.

**Decisões arquiteturais principais:**
- Percentual (`Percentage`) é o único campo financeiro persistido; valor limite = renda × percentual (sempre calculado)
- Saldo consumido = soma de transações `Debit` + `Paid` das categorias vinculadas no mês (query agregada)
- Renda mensal = soma de transações `Credit` + `Paid` no mês (reutiliza padrão do `DashboardRepository`)
- Unicidade de categoria por mês garantida por constraint de banco `UNIQUE(category_id, reference_year, reference_month)`
- Nome de orçamento único globalmente por constraint `UNIQUE(name)`
- Categorias inativas são automaticamente removidas de orçamentos recorrentes ao gerar o próximo mês

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Frontend (React)                                                       │
│  features/budgets/ → api/ hooks/ components/ pages/ types/ schemas/     │
│  Novo item no Sidebar: "Orçamentos" (/budgets)                          │
└────────────────────────────┬────────────────────────────────────────────┘
                             │ HTTP (Axios → apiClient)
┌────────────────────────────▼────────────────────────────────────────────┐
│  API Layer (BudgetsController)                                          │
│  POST/PUT/DELETE/GET api/v1/budgets                                     │
│  GET api/v1/budgets/summary                                             │
│  GET api/v1/budgets/available-percentage                                │
└────────────────────────────┬────────────────────────────────────────────┘
                             │ IDispatcher
┌────────────────────────────▼────────────────────────────────────────────┐
│  Application Layer                                                      │
│  Commands: Create, Update, Delete Budget                                │
│  Queries: List, GetById, GetSummary, GetAvailablePercentage             │
│  DTOs: BudgetResponse, BudgetSummaryResponse                            │
│  Validators: CreateBudgetValidator, UpdateBudgetValidator               │
└──────────┬─────────────────────────────┬────────────────────────────────┘
           │                             │
┌──────────▼──────────┐    ┌─────────────▼────────────────────────────────┐
│  Domain Layer        │    │  Infra Layer                                 │
│  Entity: Budget      │    │  BudgetRepository, BudgetConfiguration       │
│  IBudgetRepository   │    │  BudgetRecurrenceWorker (BackgroundService)  │
│  BudgetDomainService │    │  EF Core Migration                           │
│  Domain Exceptions   │    │  GlobalExceptionHandler (novos mappings)     │
└──────────────────────┘    └─────────────────────────────────────────────┘
```

**Componentes e responsabilidades:**

| Componente | Camada | Responsabilidade |
|---|---|---|
| `Budget` | Domain | Entidade com nome, percentual, mês de referência, flag de recorrência e coleção de category IDs |
| `BudgetDomainService` | Domain | Validação de teto de 100%, unicidade de categoria por mês, regras de período |
| `IBudgetRepository` | Domain | Interface do repositório com queries específicas de orçamento |
| `BudgetRepository` | Infra | Implementação EF Core com queries otimizadas para cálculos agregados |
| `BudgetConfiguration` | Infra | Fluent API para tabelas `budgets` e `budget_categories` |
| `BudgetRecurrenceWorker` | Infra | BackgroundService que replica orçamentos recorrentes mensalmente |
| `BudgetsController` | API | Endpoints REST para CRUD e consultas de orçamento |
| `features/budgets/` | Frontend | Módulo completo com dashboard, formulário e hooks React Query |

**Fluxo de dados principal (Criação):**
1. Frontend envia `POST /api/v1/budgets` com `{ name, percentage, referenceYear, referenceMonth, categoryIds, isRecurrent }`
2. Controller cria `CreateBudgetCommand` e despacha via `IDispatcher`
3. Handler valida via `CreateBudgetValidator` (FluentValidation)
4. Handler chama `BudgetDomainService.ValidatePercentageCap()` para verificar teto de 100%
5. Handler chama `BudgetDomainService.ValidateCategoryUniqueness()` para verificar unicidade de categorias no mês
6. Entidade `Budget.Create()` é instanciada e persistida via `IBudgetRepository`
7. Retorna `BudgetResponse` com dados calculados (renda, limite, consumido)

**Fluxo de dados principal (Dashboard):**
1. Frontend chama `GET /api/v1/budgets/summary?month=X&year=Y`
2. Query handler consulta todos os orçamentos do mês
3. Para cada orçamento, calcula renda, limite e consumido via queries agregadas no `IBudgetRepository`
4. Retorna `BudgetSummaryResponse` com consolidado e lista de cards

---

## Design de Implementação

### Interfaces Principais

```csharp
// Domain Layer - Interface do repositório
public interface IBudgetRepository : IRepository<Budget>
{
    Task<IReadOnlyList<Budget>> GetByMonthAsync(
        int year, int month, CancellationToken cancellationToken);

    Task<Budget?> GetByIdWithCategoriesAsync(
        Guid id, CancellationToken cancellationToken);

    Task<decimal> GetTotalPercentageForMonthAsync(
        int year, int month, Guid? excludeBudgetId,
        CancellationToken cancellationToken);

    Task<bool> IsCategoryUsedInMonthAsync(
        Guid categoryId, int year, int month,
        Guid? excludeBudgetId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetUsedCategoryIdsForMonthAsync(
        int year, int month, Guid? excludeBudgetId,
        CancellationToken cancellationToken);

    Task<decimal> GetMonthlyIncomeAsync(
        int year, int month, CancellationToken cancellationToken);

    Task<decimal> GetConsumedAmountAsync(
        IReadOnlyList<Guid> categoryIds, int year, int month,
        CancellationToken cancellationToken);

    Task<decimal> GetUnbudgetedExpensesAsync(
        int year, int month, CancellationToken cancellationToken);

    Task<IReadOnlyList<Budget>> GetRecurrentBudgetsForMonthAsync(
        int year, int month, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name, Guid? excludeBudgetId,
        CancellationToken cancellationToken);

    Task RemoveCategoryFromBudgetsAsync(
        Guid categoryId, CancellationToken cancellationToken);

    void Remove(Budget budget);
}
```

```csharp
// Domain Layer - Domain Service
public class BudgetDomainService
{
    public async Task ValidatePercentageCapAsync(
        IBudgetRepository repository,
        int year, int month,
        decimal newPercentage,
        Guid? excludeBudgetId,
        CancellationToken cancellationToken);

    public async Task ValidateCategoryUniquenessAsync(
        IBudgetRepository repository,
        IReadOnlyList<Guid> categoryIds,
        int year, int month,
        Guid? excludeBudgetId,
        CancellationToken cancellationToken);

    public void ValidateReferenceMonth(int year, int month);
}
```

### Modelos de Dados

#### Entidade de Domínio: `Budget`

```csharp
// Domain/Entity/Budget.cs
public class Budget : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public decimal Percentage { get; private set; }
    public int ReferenceYear { get; private set; }
    public int ReferenceMonth { get; private set; }
    public bool IsRecurrent { get; private set; }

    // Coleção de IDs de categorias vinculadas (materializada via tabela associativa)
    private readonly List<Guid> _categoryIds = new();
    public IReadOnlyList<Guid> CategoryIds => _categoryIds.AsReadOnly();

    public static Budget Create(
        string name,
        decimal percentage,
        int referenceYear,
        int referenceMonth,
        IReadOnlyList<Guid> categoryIds,
        bool isRecurrent,
        string userId);

    public static Budget Restore(
        Guid id,
        string name,
        decimal percentage,
        int referenceYear,
        int referenceMonth,
        IReadOnlyList<Guid> categoryIds,
        bool isRecurrent,
        string createdBy,
        DateTime createdAt,
        string? updatedBy,
        DateTime? updatedAt);

    public void Update(
        string name,
        decimal percentage,
        IReadOnlyList<Guid> categoryIds,
        bool isRecurrent,
        string userId);

    public decimal CalculateLimit(decimal monthlyIncome)
        => monthlyIncome * (Percentage / 100m);
}
```

#### Esquema de Banco de Dados

**Tabela `budgets`:**

| Coluna | Tipo | Constraint |
|---|---|---|
| `id` | `uuid` | PK, DEFAULT `gen_random_uuid()` |
| `name` | `varchar(150)` | NOT NULL, UNIQUE |
| `percentage` | `numeric(5,2)` | NOT NULL, CHECK > 0 AND <= 100 |
| `reference_year` | `smallint` | NOT NULL |
| `reference_month` | `smallint` | NOT NULL, CHECK 1-12 |
| `is_recurrent` | `boolean` | NOT NULL, DEFAULT false |
| `created_by` | `varchar(100)` | NOT NULL |
| `created_at` | `timestamptz` | NOT NULL, DEFAULT NOW() |
| `updated_by` | `varchar(100)` | NULL |
| `updated_at` | `timestamptz` | NULL |

**Tabela `budget_categories` (associativa):**

| Coluna | Tipo | Constraint |
|---|---|---|
| `budget_id` | `uuid` | PK (composta), FK → `budgets.id` ON DELETE CASCADE |
| `category_id` | `uuid` | PK (composta), FK → `categories.id` ON DELETE CASCADE |

**Índices:**
- `ix_budgets_reference` — `(reference_year, reference_month)` para queries de listagem por mês
- `ix_budget_categories_category_month` — `UNIQUE(category_id, reference_year, reference_month)` via join com `budgets` (implementado como unique index na tabela `budget_categories` com colunas desnormalizadas `reference_year` e `reference_month` ou constraint aplicada em nível de aplicação + fallback com query)

> **Nota sobre unicidade de categoria por mês:** Como a tabela associativa `budget_categories` não possui as colunas de mês diretamente, a constraint de unicidade `(category_id, reference_year, reference_month)` será garantida a nível de aplicação pelo `BudgetDomainService.ValidateCategoryUniquenessAsync()` e opcionalmente por uma unique constraint na tabela associativa que inclua `reference_year` e `reference_month` como colunas desnormalizadas para segurança adicional. A abordagem recomendada é desnormalizar `reference_year` e `reference_month` na tabela `budget_categories` para permitir a constraint de unicidade em nível de banco:

**Tabela `budget_categories` (versão final com desnormalização):**

| Coluna | Tipo | Constraint |
|---|---|---|
| `budget_id` | `uuid` | PK (composta), FK → `budgets.id` ON DELETE CASCADE |
| `category_id` | `uuid` | FK → `categories.id` ON DELETE CASCADE |
| `reference_year` | `smallint` | NOT NULL |
| `reference_month` | `smallint` | NOT NULL |

**Constraint:** `UNIQUE(category_id, reference_year, reference_month)` — impede a mesma categoria em dois orçamentos do mesmo mês em nível de banco.

#### DTOs de Response

```csharp
// Application/Dtos/BudgetResponse.cs
public record BudgetResponse(
    Guid Id,
    string Name,
    decimal Percentage,
    int ReferenceYear,
    int ReferenceMonth,
    bool IsRecurrent,
    decimal MonthlyIncome,          // Calculado: soma Credit+Paid do mês
    decimal LimitAmount,            // Calculado: MonthlyIncome × (Percentage/100)
    decimal ConsumedAmount,         // Calculado: soma Debit+Paid das categorias no mês
    decimal RemainingAmount,        // Calculado: LimitAmount - ConsumedAmount
    decimal ConsumedPercentage,     // Calculado: (ConsumedAmount / LimitAmount) × 100
    IReadOnlyList<BudgetCategoryDto> Categories,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record BudgetCategoryDto(
    Guid Id,
    string Name
);

// Application/Dtos/BudgetSummaryResponse.cs
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
    decimal UnbudgetedExpenses,      // Gastos em categorias sem orçamento
    IReadOnlyList<BudgetResponse> Budgets
);

// Application/Dtos/AvailablePercentageResponse.cs
public record AvailablePercentageResponse(
    decimal UsedPercentage,
    decimal AvailablePercentage,
    IReadOnlyList<Guid> UsedCategoryIds
);
```

#### Request DTOs

```csharp
// Controllers/Requests/CreateBudgetRequest.cs
public record CreateBudgetRequest(
    string Name,
    decimal Percentage,
    int ReferenceYear,
    int ReferenceMonth,
    List<Guid> CategoryIds,
    bool IsRecurrent = false
);

// Controllers/Requests/UpdateBudgetRequest.cs
public record UpdateBudgetRequest(
    string Name,
    decimal Percentage,
    List<Guid> CategoryIds,
    bool IsRecurrent
);
```

### Endpoints de API

| Método | Rota | Descrição | Request | Response | Status |
|---|---|---|---|---|---|
| `POST` | `/api/v1/budgets` | Criar orçamento | `CreateBudgetRequest` | `BudgetResponse` | 201 |
| `PUT` | `/api/v1/budgets/{id}` | Editar orçamento | `UpdateBudgetRequest` | `BudgetResponse` | 200 |
| `DELETE` | `/api/v1/budgets/{id}` | Excluir orçamento | — | — | 204 |
| `GET` | `/api/v1/budgets/{id}` | Obter orçamento por ID | — | `BudgetResponse` | 200 |
| `GET` | `/api/v1/budgets?month=X&year=Y` | Listar orçamentos do mês | QueryString | `IReadOnlyList<BudgetResponse>` | 200 |
| `GET` | `/api/v1/budgets/summary?month=X&year=Y` | Dashboard consolidado | QueryString | `BudgetSummaryResponse` | 200 |
| `GET` | `/api/v1/budgets/available-percentage?month=X&year=Y&excludeBudgetId=Z` | Percentual disponível e categorias em uso | QueryString | `AvailablePercentageResponse` | 200 |

**Regras de autorização:** Todos os endpoints requerem `[Authorize]`. Sem distinção de role (Admin/Member).

**Validações por endpoint:**
- `POST`: nome obrigatório (2-150 chars), percentual > 0 e ≤ 100, mês/ano válidos, mês corrente ou futuro, ao menos 1 categoria, categorias devem ser do tipo Despesa
- `PUT`: mesmas validações + orçamento deve ser do mês corrente ou futuro
- `DELETE`: orçamento deve ser do mês corrente ou futuro

---

## Pontos de Integração

### Impacto na Exclusão/Migração de Categorias

O handler `DeleteCategoryCommandHandler` existente precisa ser estendido para desassociar a categoria de orçamentos antes de excluí-la. O SQL de migração em `CategoryRepository.MigrateLinkedDataAsync()` também precisa ser atualizado.

**Alterações necessárias:**
1. `ICategoryRepository.HasLinkedDataAsync()` — incluir check em `budget_categories`
2. `CategoryRepository.MigrateLinkedDataAsync()` — incluir `DELETE FROM budget_categories WHERE category_id = @sourceCategoryId` (desassociação, não migração — orçamentos ficam sem a categoria)
3. `DeleteCategoryCommandHandler` — após migração/remoção, verificar se algum orçamento ficou sem categorias e logar warning

### Background Service: `BudgetRecurrenceWorker`

Novo `BackgroundService` separado do `RecurrenceMaintenanceWorker` existente. Executa uma vez por dia (mesma estratégia do worker existente). No início de cada mês, detecta orçamentos recorrentes do mês anterior que ainda não foram replicados e cria cópias para o mês corrente.

**Lógica:**
1. Busca orçamentos com `IsRecurrent = true` do mês anterior ao corrente
2. Para cada orçamento, verifica se já existe orçamento com mesmo nome no mês corrente
3. Se não existe, cria cópia com mesmas categorias (excluindo categorias inativas), mesmo percentual e `IsRecurrent = true`
4. Se soma dos percentuais > 100%, cria mesmo assim (conforme PRD) — log de warning

---

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
|---|---|---|---|
| `FinanceiroDbContext` | Nova DbSet | Adiciona `DbSet<Budget>`. Baixo risco. | Adicionar propriedade |
| `DeleteCategoryCommandHandler` | Mudança de comportamento | Deve desassociar categoria de orçamentos ao excluir. Risco médio. | Alterar handler + repository |
| `CategoryRepository.MigrateLinkedDataAsync` | Mudança SQL | Adiciona DELETE em `budget_categories`. Risco médio. | Alterar SQL |
| `CategoryRepository.HasLinkedDataAsync` | Mudança de query | Adiciona check em `budget_categories`. Baixo risco. | Alterar query |
| `GlobalExceptionHandler` | Novos mappings | Adicionar mappings para novas domain exceptions. Baixo risco. | Adicionar cases no switch |
| `ApplicationServiceExtensions` | Novos registros DI | Registrar handlers, validators, domain service. Baixo risco. | Adicionar registros |
| `ServiceCollectionExtensions` (Infra) | Novos registros DI | Registrar `IBudgetRepository`, `BudgetRecurrenceWorker`. Baixo risco. | Adicionar registros |
| `Sidebar.tsx` / `constants.ts` | Novo item de navegação | Adicionar "Orçamentos" no menu lateral. Baixo risco. | Adicionar item |
| `routes.tsx` | Nova rota | Adicionar `/budgets`. Baixo risco. | Adicionar rota |
| `MappingConfig.cs` | Novo mapeamento | Adicionar `Budget → BudgetResponse`. Baixo risco. | Adicionar config |

---

## Abordagem de Testes

### Testes Unitários

**Domain Layer:**
- `Budget.Create()` — validação de parâmetros, valores padrão
- `Budget.Update()` — atualização de campos, auditoria
- `Budget.CalculateLimit()` — cálculo correto de valor limite
- `BudgetDomainService.ValidatePercentageCapAsync()` — teto de 100%, edge cases (exatamente 100%, exceder por 0.01%)
- `BudgetDomainService.ValidateCategoryUniquenessAsync()` — categoria duplicada, categoria em outro orçamento
- `BudgetDomainService.ValidateReferenceMonth()` — mês passado rejeitado, mês corrente aceito, mês futuro aceito

**Application Layer:**
- `CreateBudgetCommandHandler` — fluxo completo com mocks de repositório
- `UpdateBudgetCommandHandler` — atualização com validação de teto
- `DeleteBudgetCommandHandler` — exclusão com validação de período
- `GetBudgetSummaryQueryHandler` — cálculos consolidados corretos
- `CreateBudgetValidator` — validação FluentValidation de todos os campos
- `UpdateBudgetValidator` — validação FluentValidation de todos os campos

**Mocks necessários:** `IBudgetRepository` (NSubstitute), `ICategoryRepository`, `IUnitOfWork`, `IAuditService`, `ILogger<T>`

**Cenários críticos:**
- Criar orçamento que atinge exatamente 100%
- Tentar criar orçamento que excede 100%
- Criar orçamento com categoria já usada no mês
- Editar orçamento de mês passado (deve rejeitar)
- Calcular consumido quando não há transações
- Calcular consumido com transações canceladas (devem ser ignoradas)
- Calcular renda com transações de ajuste

### Testes de Integração

**Repository (Testcontainers + PostgreSQL):**
- `BudgetRepository.GetByMonthAsync()` — filtragem correta por mês
- `BudgetRepository.GetTotalPercentageForMonthAsync()` — soma correta excluindo budget específico
- `BudgetRepository.IsCategoryUsedInMonthAsync()` — detecção correta de conflito
- `BudgetRepository.GetConsumedAmountAsync()` — soma correta filtrando por tipo, status e mês
- `BudgetRepository.GetMonthlyIncomeAsync()` — soma correta de receitas pagas
- `BudgetRepository.GetUnbudgetedExpensesAsync()` — gastos fora de orçamentos
- Constraint UNIQUE de `(category_id, reference_year, reference_month)` — violação gera exceção

**HTTP Integration (WebApplicationFactory):**
- CRUD completo via HTTP
- Validação de responses (ProblemDetails em erros)
- Autenticação obrigatória
- Filtros de mês/ano nos endpoints de listagem e summary

### Testes Frontend

**Componentes (Vitest + Testing Library):**
- `BudgetCard` — renderização correta de cores por faixa (verde/amarelo/vermelho), badge "Estourado"
- `BudgetForm` — validação de campos, seleção de categorias, categorias desabilitadas
- `BudgetDashboard` — resumo consolidado, filtro de mês, empty state
- `useBudgets` / `useBudgetSummary` — hooks React Query com MSW

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

1. **Domain Layer** — Entidade `Budget`, `IBudgetRepository`, `BudgetDomainService`, Domain Exceptions
   - *Primeiro porque é o core sem dependências externas*

2. **Infra Layer — Persistência** — `BudgetConfiguration`, `BudgetRepository`, Migration EF Core, `FinanceiroDbContext.Budgets`
   - *Depende do Domain; necessário para testar persistence layer*

3. **Application Layer — Commands** — `CreateBudgetCommand/Handler/Validator`, `UpdateBudgetCommand/Handler/Validator`, `DeleteBudgetCommand/Handler`
   - *Depende do Domain e interfaces; testável com mocks*

4. **Application Layer — Queries** — `ListBudgetsQuery`, `GetBudgetByIdQuery`, `GetBudgetSummaryQuery`, `GetAvailablePercentageQuery`, DTOs
   - *Depende do repositório para cálculos agregados*

5. **API Layer** — `BudgetsController`, Request DTOs, registros DI, `GlobalExceptionHandler` mappings
   - *Depende dos Command/Query handlers*

6. **Impacto em Category** — Alterações em `DeleteCategoryCommandHandler`, `CategoryRepository`
   - *Pode ser feito em paralelo com etapas 3-5*

7. **Background Service** — `BudgetRecurrenceWorker`
   - *Feature independente, pode ser implementada após o CRUD*

8. **Frontend** — Módulo `features/budgets/` completo (types, api, hooks, schemas, components, pages, rota, sidebar)
   - *Depende da API estar funcional*

9. **Testes de Integração e E2E** — Testcontainers, WebApplicationFactory, MSW
   - *Fase final de validação*

### Dependências Técnicas

- **Docker** com PostgreSQL disponível para testes de integração e migrations
- **EF Core Migration** deve ser gerada e testada antes dos testes de integração
- **Nenhuma dependência externa** nova — todas as bibliotecas já estão no projeto (FluentValidation, Mapster, EF Core, etc.)

---

## Monitoramento e Observabilidade

- **Logs estruturados** via `ILogger<T>` (padrão existente):
  - `LogInformation` para operações CRUD bem-sucedidas
  - `LogWarning` para tentativas de exceder 100%, categorias desabilitadas em recorrência
  - `LogError` para falhas no `BudgetRecurrenceWorker`
- **Auditoria** via `IAuditService` existente para criação, edição e exclusão de orçamentos
- **Health check**: nenhuma alteração — o `BudgetRecurrenceWorker` usa o mesmo padrão do `RecurrenceMaintenanceWorker` (resiliente a falhas, retry no próximo ciclo)

---

## Considerações Técnicas

### Decisões Principais

| Decisão | Justificativa | Alternativas Rejeitadas |
|---|---|---|
| Não materializar saldos | Simplicidade, consistência automática quando transações mudam | Tabela de saldo com triggers — complexidade desnecessária para volume esperado |
| Campos `Year`/`Month` separados (int) | Consistente com `DashboardRepository`, facilita queries e comparações | `DateOnly` — requer conversão constante, menos intuitivo em queries |
| Desnormalizar `reference_year/month` em `budget_categories` | Permite constraint UNIQUE em nível de banco para unicidade de categoria/mês | Validação apenas em aplicação — risco de race condition |
| Novo worker separado (`BudgetRecurrenceWorker`) | Separação de responsabilidades, ciclo de vida independente | Mesmo worker — acoplamento com transações recorrentes |
| Nome de orçamento único globalmente | Decisão do produto — simplifica identificação | Único por mês — permitiria confusão em consultas históricas |
| Categorias inativas removidas da recorrência | Decisão do produto — evita orçamentos com categorias inválidas | Manter e sinalizar — complexidade de UI sem ganho claro |
| Usar `decimal(5,2)` para percentual | Precision adequada (0.01 a 100.00), consistente com conceito de porcentagem | `decimal(18,2)` — over-engineering para percentual |

### Riscos Conhecidos

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Performance da query de saldo consumido com muitas transações | Baixa | Médio | Índice composto `(category_id, competence_date, status, type)` na tabela `transactions`; queries assíncronas paralelas |
| Race condition na criação simultânea de orçamentos no mesmo mês | Baixa | Baixo | Constraint UNIQUE no banco como fallback; transaction isolation |
| Recorrência gerando orçamento com >100% de percentual | Média | Baixo | PRD permite — log warning + sinalização no frontend |
| Exclusão de categoria deixando orçamento sem categorias | Baixa | Médio | Log warning + sinalização visual no dashboard; orçamento com 0 categorias é válido mas sinalizado |

### Requisitos Especiais

**Performance:**
- Adicionar índice composto em `transactions`: `(category_id, competence_date, status)` se não existente (já existe `ix_transactions_category_id` simples)
- Consultas de renda e consumido devem usar `AsNoTracking()` e projeções otimizadas
- O endpoint `GET /summary` pode agregar múltiplas queries paralelas (`Task.WhenAll`) para renda, consumido por orçamento e gastos fora de orçamento

**Segurança:**
- Sem requisitos adicionais além do `[Authorize]` padrão
- Validação de input via FluentValidation em todos os commands

### Conformidade com Padrões

- [x] Segue Clean Architecture em 4 camadas (conforme `dotnet-architecture.md`)
- [x] CQRS simplificado com `ICommand<T>`, `IQuery<T>`, `IDispatcher` (conforme padrão existente)
- [x] Repository Pattern com `IRepository<T>` base (conforme `dotnet-architecture.md`)
- [x] FluentValidation para validação de commands (conforme padrão existente)
- [x] Mapster para mapeamento Entity → DTO (conforme `MappingConfig.cs`)
- [x] Domain Exceptions herdando de `DomainException` (conforme padrão existente)
- [x] `GlobalExceptionHandler` mapeia novas exceptions (conforme padrão existente)
- [x] Endpoints REST com versionamento `api/v1/` (conforme `restful.md`)
- [x] ProblemDetails RFC 7807 para erros (conforme `restful.md`)
- [x] Testes unitários com xUnit + NSubstitute AAA pattern (conforme `dotnet-testing.md`)
- [x] Testes de integração com Testcontainers (conforme `dotnet-testing.md`)
- [x] Feature-based architecture no frontend (conforme `react-project-structure.md`)
- [x] React Hook Form + Zod para formulários (conforme padrão existente)
- [x] TanStack React Query para server state (conforme padrão existente)
- [x] Auditoria via `IAuditService` + `BaseEntity` (conforme padrão existente)

### Domain Exceptions Novas

```csharp
// Orçamento não encontrado
public class BudgetNotFoundException : DomainException { }

// Percentual excede 100% no mês
public class BudgetPercentageExceededException : DomainException { }

// Categoria já vinculada a outro orçamento no mês
public class CategoryAlreadyBudgetedException : DomainException { }

// Tentativa de editar/excluir orçamento de mês passado
public class BudgetPeriodLockedException : DomainException { }

// Orçamento sem categorias
public class BudgetMustHaveCategoriesException : DomainException { }

// Nome de orçamento já existe
public class BudgetNameAlreadyExistsException : DomainException { }

// Categoria não é do tipo Despesa
public class InvalidBudgetCategoryTypeException : DomainException { }
```

### Estrutura Frontend

```
frontend/src/features/budgets/
├── api/
│   └── budgetsApi.ts              # Funções Axios (create, update, delete, list, summary, availablePercentage)
├── components/
│   ├── BudgetCard.tsx             # Card individual com barra de progresso
│   ├── BudgetDashboard.tsx        # Dashboard consolidado com cards e resumo
│   ├── BudgetForm.tsx             # Formulário de criação/edição (React Hook Form + Zod)
│   ├── BudgetFormDialog.tsx       # Dialog/Sheet wrapper para o formulário
│   ├── BudgetSummaryHeader.tsx    # Resumo no topo (renda, orçado, gasto, restante)
│   └── MonthYearFilter.tsx        # Filtro de mês/ano (reutilizável)
├── hooks/
│   ├── useBudgets.ts              # useQuery para listar orçamentos
│   ├── useBudgetSummary.ts        # useQuery para dashboard summary
│   ├── useAvailablePercentage.ts  # useQuery para percentual disponível
│   ├── useCreateBudget.ts         # useMutation para criar
│   ├── useUpdateBudget.ts         # useMutation para editar
│   └── useDeleteBudget.ts         # useMutation para excluir
├── pages/
│   └── BudgetsPage.tsx            # Página principal (lazy-loaded)
├── schemas/
│   └── budgetSchema.ts            # Schema Zod para validação de form
├── types/
│   └── index.ts                   # Tipos TypeScript (Budget, BudgetSummary, etc.)
├── test/
│   ├── BudgetCard.test.tsx
│   ├── BudgetForm.test.tsx
│   ├── BudgetDashboard.test.tsx
│   └── handlers.ts                # MSW handlers para testes
└── index.ts                       # Barrel exports
```
