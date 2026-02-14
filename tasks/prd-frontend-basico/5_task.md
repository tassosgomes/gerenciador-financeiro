---
status: pending
parallelizable: true
blocked_by: ["3.0", "4.0"]
---

<task_context>
<domain>frontend/dashboard + backend/dashboard</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>recharts, tanstack-query, dotnet_backend</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 5.0: Dashboard (Backend + Frontend)

## Visão Geral

Implementar a feature de Dashboard completa, incluindo: (1) dois novos endpoints de agregação no backend .NET (`/dashboard/summary` e `/dashboard/charts`); e (2) a página de dashboard no frontend com KPI cards, seletor de mês/ano, gráfico de barras (receita vs despesa dos últimos 6 meses), gráfico donut (despesas por categoria do mês) e tabela de transações recentes. Esta é a tela principal do sistema — a primeira coisa que o usuário vê após login.

## Requisitos

### Backend (Fase 2.1)
- Endpoint `GET /api/v1/dashboard/summary?month=M&year=Y` retornando: saldo total, receitas do mês, despesas do mês, dívida de cartão
- Endpoint `GET /api/v1/dashboard/charts?month=M&year=Y` retornando: receita vs despesa dos últimos 6 meses + despesas por categoria do mês
- Queries com SQL agregado via LINQ no `FinanceiroDbContext`

### Frontend (PRD F2)
- PRD req. 7: Card "Saldo total" — soma dos saldos de todas as contas ativas
- PRD req. 8: Card "Total receitas do mês" — transações Credit + Paid no mês de competência
- PRD req. 9: Card "Total despesas do mês" — transações Debit + Paid no mês de competência
- PRD req. 10: Card "Dívida de cartão total" — soma dos saldos negativos de contas tipo Cartão
- PRD req. 11: Gráfico de barras — Receita vs Despesa últimos 6 meses
- PRD req. 12: Gráfico de pizza/donut — Despesa por categoria mês atual
- PRD req. 13: Carregar dados via endpoints específicos (não todas as transações)
- PRD req. 14: Mês de referência configurável (seletor de mês/ano)
- Layout fiel ao mockup `screen-examples/dashboard/index.html`

## Subtarefas

### Backend

- [ ] 5.1 Criar `DashboardSummaryResponse` DTO: `TotalBalance`, `MonthlyIncome`, `MonthlyExpenses`, `CreditCardDebt`
- [ ] 5.2 Criar `DashboardChartsResponse` DTO: `RevenueVsExpense` (lista de `MonthlyComparisonDto`), `ExpenseByCategory` (lista de `CategoryExpenseDto`)
- [ ] 5.3 Criar `GetDashboardSummaryQuery` (MediatR) com parâmetros `Month` e `Year`
- [ ] 5.4 Implementar `GetDashboardSummaryQueryHandler` — saldo total (soma de Account.Balance onde IsActive), receitas (soma de Transaction.Amount onde Type=Credit, Status=Paid, CompetenceDate no mês/ano), despesas (idem Debit), dívida cartão (soma de saldos negativos de contas tipo Cartão)
- [ ] 5.5 Criar `GetDashboardChartsQuery` (MediatR) com parâmetros `Month` e `Year`
- [ ] 5.6 Implementar `GetDashboardChartsQueryHandler` — receita vs despesa agrupado por mês nos últimos 6 meses; despesas agrupadas por categoria no mês selecionado com percentual
- [ ] 5.7 Criar `DashboardController` com 2 endpoints GET, decorados com `[Authorize]`
- [ ] 5.8 Testes unitários dos handlers (mock do DbContext ou in-memory database)

### Frontend

- [ ] 5.9 Criar `src/features/dashboard/types/dashboard.ts` — interfaces: `DashboardSummary`, `MonthlyComparison`, `CategoryExpense`, `DashboardCharts`
- [ ] 5.10 Criar `src/features/dashboard/api/dashboardApi.ts` — funções `getDashboardSummary(month, year)` e `getDashboardCharts(month, year)` usando apiClient
- [ ] 5.11 Criar `src/features/dashboard/hooks/useDashboard.ts` — hooks TanStack Query: `useDashboardSummary(month, year)` e `useDashboardCharts(month, year)` com staleTime 5 min
- [ ] 5.12 Criar `src/features/dashboard/components/SummaryCards.tsx` — grid 4 cards (Saldo Total com ícone wallet azul, Receitas com ícone seta verde, Despesas com ícone seta vermelho, Dívida Cartão com ícone amarelo); estados loading (skeleton) e valores formatados em R$
- [ ] 5.13 Criar `src/features/dashboard/components/MonthNavigator.tsx` — seletor com setas esquerda/direita e label "Outubro 2023" no meio; usa `formatCompetenceMonth`
- [ ] 5.14 Criar `src/features/dashboard/components/RevenueExpenseChart.tsx` — usa BarChartWidget com dados de `revenueVsExpense`; barras verdes (receita) e vermelhas (despesa); labels dos meses em pt-BR
- [ ] 5.15 Criar `src/features/dashboard/components/CategoryExpenseChart.tsx` — usa DonutChartWidget com dados de `expenseByCategory`; legenda com nome da categoria e percentual
- [ ] 5.16 Criar `src/features/dashboard/components/RecentTransactions.tsx` — tabela com as 5 transações mais recentes do mês (buscar de `/transactions?page=1&size=5&...`)
- [ ] 5.17 Criar `src/features/dashboard/pages/DashboardPage.tsx` — composição: saudação ("Bom dia, {nome}!"), MonthNavigator, SummaryCards, grid com RevenueExpenseChart + CategoryExpenseChart + RecentTransactions
- [ ] 5.18 Criar MSW handlers: mock de GET `/api/v1/dashboard/summary` e GET `/api/v1/dashboard/charts`
- [ ] 5.19 Testes: SummaryCards (renderiza dados, loading, erro), DashboardPage (composição completa com mock)

## Sequenciamento

- Bloqueado por: 3.0 (Auth — rota protegida), 4.0 (Backend — CORS + base)
- Desbloqueia: 10.0 (Polimento)
- Paralelizável: Sim, com 6.0 (Contas), 7.0 (Categorias), 9.0 (Admin)

## Detalhes de Implementação

### Backend — Summary Query Handler

```csharp
public async Task<DashboardSummaryResponse> Handle(
    GetDashboardSummaryQuery request, CancellationToken ct)
{
    var totalBalance = await _context.Accounts
        .Where(a => a.IsActive)
        .SumAsync(a => a.Balance, ct);

    var startDate = new DateTime(request.Year, request.Month, 1);
    var endDate = startDate.AddMonths(1);

    var monthlyIncome = await _context.Transactions
        .Where(t => t.Type == TransactionType.Credit
                 && t.Status == TransactionStatus.Paid
                 && t.CompetenceDate >= startDate
                 && t.CompetenceDate < endDate)
        .SumAsync(t => (decimal?)t.Amount ?? 0, ct);

    var monthlyExpenses = await _context.Transactions
        .Where(t => t.Type == TransactionType.Debit
                 && t.Status == TransactionStatus.Paid
                 && t.CompetenceDate >= startDate
                 && t.CompetenceDate < endDate)
        .SumAsync(t => (decimal?)t.Amount ?? 0, ct);

    var creditCardDebt = await _context.Accounts
        .Where(a => a.Type == AccountType.Cartao && a.Balance < 0)
        .SumAsync(a => (decimal?)a.Balance ?? 0, ct);

    return new DashboardSummaryResponse(
        totalBalance, monthlyIncome, monthlyExpenses, Math.Abs(creditCardDebt));
}
```

### Frontend — Layout da DashboardPage (mockup referência)

```
┌─────────────────────────────────────────────────────────┐
│ Bom dia, Carlos!                    [◀ Outubro 2023 ▶] │
│ Aqui está o resumo financeiro...                        │
├─────────────┬─────────────┬─────────────┬──────────────┤
│ Saldo Total │ Receitas    │ Despesas    │ Dívida       │
│ R$ 12.450   │ R$ 8.200    │ R$ 4.100    │ R$ 1.450     │
│ +2.5%       │ Meta: 10k   │ 50% do orç. │ 2 cartões    │
├─────────────┴─────────────┴─────────────┴──────────────┤
│ ┌──────────────────────┐  ┌───────────────────────────┐ │
│ │ Receita vs Despesa   │  │ Despesas por Categoria    │ │
│ │ (BarChart 6 meses)   │  │ (DonutChart)              │ │
│ └──────────────────────┘  └───────────────────────────┘ │
├─────────────────────────────────────────────────────────┤
│ Transações Recentes                                     │
│ ┌──────────────────────────────────────────────────────┐│
│ │ Data | Descrição | Categoria | Valor | Status       ││
│ └──────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
```

## Critérios de Sucesso

### Backend
- `GET /api/v1/dashboard/summary?month=1&year=2026` retorna JSON com os 4 campos corretos
- `GET /api/v1/dashboard/charts?month=1&year=2026` retorna dados de 6 meses + categorias
- Endpoints protegidos por `[Authorize]`
- Testes unitários dos handlers passam
- `dotnet build` e `dotnet test` sem erros

### Frontend
- Dashboard carrega ao acessar `/dashboard` com dados dos endpoints
- 4 KPI cards exibem valores formatados em R$ com ícones corretos
- Seletor de mês navega entre meses e atualiza todos os dados
- Gráfico de barras mostra receita (verde) vs despesa (vermelho) dos últimos 6 meses
- Gráfico donut mostra despesas por categoria com legenda
- Tabela de transações recentes exibe as 5 mais recentes
- Skeleton loaders são exibidos durante o carregamento
- Layout fiel ao mockup `screen-examples/dashboard/`
- Testes unitários dos componentes passam
