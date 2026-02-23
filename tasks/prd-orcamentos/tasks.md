```markdown
# Resumo de Tarefas de Implementação de Orçamentos

## Visão Geral

Implementação da feature de Orçamentos (Budgets), que adiciona uma camada de planejamento financeiro ao GestorFinanceiro. Permite criar envelopes de gasto com percentual da renda mensal, associar categorias de despesa, acompanhar o consumo em tempo real via dashboard com barras de progresso e indicadores visuais, e replicar orçamentos automaticamente via recorrência mensal. Todos os valores (renda, limite, consumido) são calculados em tempo de consulta — sem materialização de saldos.

## Fases de Implementação

### Fase 1 — Domain (Fundação)
Criação da entidade `Budget`, interface `IBudgetRepository`, `BudgetDomainService` e 7 domain exceptions. Nenhuma dependência de infra — totalmente testável unitariamente.

### Fase 2 — Infra (Persistência)
Migration EF Core para tabelas `budgets` e `budget_categories` (com desnormalização de year/month), `BudgetConfiguration` (Fluent API), `BudgetRepository` com queries agregadas, `FinanceiroDbContext.Budgets`.

### Fase 3 — Application (Orquestração)
Commands (Create, Update, Delete) com FluentValidation e Queries (List, GetById, Summary, AvailablePercentage) com DTOs de response. Padrão CQRS simplificado via `IDispatcher`.

### Fase 4 — API e Integrações
`BudgetsController` com 7 endpoints REST, Request DTOs, registros DI, mappings no `GlobalExceptionHandler`. Alterações em `CategoryRepository`/`DeleteCategoryCommandHandler` para desassociação de categorias. `BudgetRecurrenceWorker` como BackgroundService.

### Fase 5 — Frontend (Interface)
Módulo `features/budgets/` completo: tipos TypeScript, API client, hooks React Query, schemas Zod, componentes (BudgetCard, BudgetDashboard, BudgetForm, BudgetSummaryHeader, MonthYearFilter), página, rota e item no sidebar.

### Fase 6 — Testes de Integração e Validação
Testes de integração backend (Repository com Testcontainers, HTTP com WebApplicationFactory) e testes frontend (Vitest + Testing Library + MSW).

## Tarefas

- [x] 1.0 Domain Layer — Entidade Budget, Interfaces, Domain Service e Exceptions
- [x] 2.0 Infra Layer — Migration EF Core, BudgetConfiguration e BudgetRepository
- [x] 3.0 Application Layer — Commands (Create, Update, Delete) e Validators
- [x] 4.0 Application Layer — Queries e DTOs de Response
- [x] 5.0 API Layer — BudgetsController, Request DTOs e Registro DI
- [x] 6.0 Impacto em Category — Desassociação de Categorias em Orçamentos
- [x] 7.0 Background Service — BudgetRecurrenceWorker
- [x] 8.0 Testes de Integração Backend (Repository + HTTP)
- [x] 9.0 Frontend — Tipos, API Client, Hooks e Schemas
- [x] 10.0 Frontend — Componentes, Páginas e Navegação
- [x] 11.0 Testes Frontend

## Análise de Paralelização

### Lanes de Execução Paralela

| Lane | Tarefas | Descrição |
|------|---------|-----------|
| Lane A (Domain → Infra) | 1.0 → 2.0 | Caminho de domínio: entidade + interfaces → persistência |
| Lane B (Application) | 3.0, 4.0 | Commands e queries — dependem de 1.0, paralelos com 2.0 e entre si |
| Lane C (API + Integrações) | 5.0, 6.0, 7.0 | Controller + integrações — 5.0 depende de 3.0/4.0; 6.0 e 7.0 dependem de 2.0 |
| Lane D (Frontend) | 9.0 → 10.0 | Tipos/hooks → componentes/páginas — depende de 5.0 |
| Lane E (Testes) | 8.0, 11.0 | Integração backend + testes frontend — fase final de validação |

### Caminho Crítico

```
1.0 → 2.0 → 3.0 → 5.0 → 9.0 → 10.0 → 11.0
```

O caminho mais longo passa por: entidade → persistência → commands → API → frontend types → frontend components → testes frontend.

### Diagrama de Dependências

```
┌───────────────────────────────────────────────────────────────┐
│  FASE 1 — Domain                                              │
│                                                               │
│  ┌─────┐                                                      │
│  │ 1.0 │ Budget Entity + IBudgetRepository + DomainService    │
│  │     │ + 7 Domain Exceptions + Testes Unitários             │
│  └──┬──┘                                                      │
└─────┼─────────────────────────────────────────────────────────┘
      │
┌─────▼─────────────────────────────────────────────────────────┐
│  FASE 2 — Infra + Application (paralelos)                     │
│                                                               │
│  ┌─────┐          ┌─────┐     ┌─────┐                        │
│  │ 2.0 │          │ 3.0 │     │ 4.0 │                        │
│  │Migr.│          │Cmds │     │Qrys │                        │
│  │Repo │          │Valid│     │DTOs │                        │
│  └──┬──┘          └──┬──┘     └──┬──┘                        │
│     │                │           │                            │
│     │  (3.0 e 4.0 paralelos com 2.0 — usam mocks)           │
└─────┼────────────────┼───────────┼────────────────────────────┘
      │                │           │
┌─────▼────────────────▼───────────▼────────────────────────────┐
│  FASE 3 — API + Integrações                                   │
│                                                               │
│  ┌─────┐     ┌─────┐     ┌─────┐                             │
│  │ 5.0 │     │ 6.0 │     │ 7.0 │                             │
│  │Ctrlr│     │Categ│     │Workr│                             │
│  │ DI  │     │Impct│     │Recur│                             │
│  └──┬──┘     └──┬──┘     └──┬──┘                             │
│     │           │           │                                 │
│     │  (5.0 depende de 3.0/4.0; 6.0 e 7.0 dependem de 2.0) │
└─────┼───────────┼───────────┼─────────────────────────────────┘
      │           │           │
┌─────▼───────────▼───────────▼─────────────────────────────────┐
│  FASE 4 — Testes Integração Backend                           │
│                                                               │
│  ┌─────┐                                                      │
│  │ 8.0 │ Repository (Testcontainers) + HTTP (WebAppFactory)   │
│  └──┬──┘                                                      │
└─────┼─────────────────────────────────────────────────────────┘
      │
┌─────▼─────────────────────────────────────────────────────────┐
│  FASE 5 — Frontend                                            │
│                                                               │
│  ┌─────┐                                                      │
│  │ 9.0 │ Tipos + API + Hooks + Schemas                        │
│  └──┬──┘                                                      │
│     │                                                         │
│  ┌──▼──┐                                                      │
│  │10.0 │ Componentes + Páginas + Rota + Sidebar               │
│  └──┬──┘                                                      │
│     │                                                         │
│  ┌──▼──┐                                                      │
│  │11.0 │ Testes Frontend (Vitest + MSW)                       │
│  └─────┘                                                      │
└───────────────────────────────────────────────────────────────┘
```
```
