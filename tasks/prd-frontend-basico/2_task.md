---
status: pending
parallelizable: false
blocked_by: ["1.0"]
---

<task_context>
<domain>frontend/ui</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>shadcn-ui, tailwindcss, react-router-dom</dependencies>
<unblocks>"3.0"</unblocks>
</task_context>

# Tarefa 2.0: Componentes Compartilhados e Layout

## Visão Geral

Implementar os componentes de layout principal (AppShell, Sidebar, Topbar) e os componentes UI reutilizáveis via Shadcn/UI. Também criar utilitários de formatação (moeda, data), hooks compartilhados e os tipos base da API. Esses componentes formam a "casca" visual da aplicação, reproduzindo fielmente os mockups das telas de referência.

## Requisitos

- Layout principal com sidebar fixa (w-64) + topbar + área de conteúdo scrollável
- Sidebar com navegação para: Dashboard, Transações, Contas, Categorias, Admin
- Sidebar com logo "GestorFinanceiro", indicador de status e perfil do usuário
- Topbar com título da página, botão de notificações e botão de logout
- Componentes Shadcn/UI instalados e customizados: Button, Card, Input, Select, Modal (Dialog), Table, Tabs, Toggle (Switch), Badge, Skeleton, Toast (Sonner)
- Wrappers de gráficos Recharts: BarChartWidget, DonutChartWidget
- Utilitários: `formatCurrency`, `formatDate`, `formatCompetenceMonth`
- Hooks: `useDebounce`, `useFormatCurrency`
- Tipos base: `PagedResponse<T>`, `ProblemDetails`
- Componente `ProtectedRoute` (placeholder — lógica de auth na tarefa 3.0)
- Navegação funcional via React Router v6 com lazy loading de rotas

## Subtarefas

- [ ] 2.1 Instalar componentes Shadcn/UI: `npx shadcn-ui@latest add button card input select dialog table tabs switch badge skeleton sonner`
- [ ] 2.2 Criar `src/shared/components/layout/Sidebar.tsx` — sidebar fixa com logo, links de navegação (Dashboard, Transações, Contas, Categorias, Admin), indicador de status, perfil do usuário; usar Material Icons conforme mockups
- [ ] 2.3 Criar `src/shared/components/layout/Topbar.tsx` — header com título da página dinâmico, botão de notificações, avatar do usuário e botão de logout
- [ ] 2.4 Criar `src/shared/components/layout/AppShell.tsx` — layout container que compõe Sidebar + Topbar + `<Outlet />` para conteúdo das rotas. Deve usar as classes exatas dos mockups (`flex h-screen overflow-hidden`)
- [ ] 2.5 Criar `src/shared/components/layout/ProtectedRoute.tsx` — wrapper de rota que verifica autenticação (placeholder: sempre autenticado, será integrado na tarefa 3.0)
- [ ] 2.6 Criar `src/shared/components/charts/BarChartWidget.tsx` — wrapper Recharts para gráfico de barras (receita vs despesa). Props: `data`, `height`, labels customizados em pt-BR
- [ ] 2.7 Criar `src/shared/components/charts/DonutChartWidget.tsx` — wrapper Recharts para gráfico de pizza/donut (despesas por categoria). Props: `data`, `height`, legenda lateral
- [ ] 2.8 Criar `src/shared/utils/formatters.ts` — funções: `formatCurrency(value: number): string` (Intl.NumberFormat pt-BR BRL), `formatDate(date: string | Date): string` (dd/MM/yyyy), `formatCompetenceMonth(month, year): string` (ex: "outubro 2026")
- [ ] 2.9 Criar `src/shared/utils/constants.ts` — constantes: `NAV_ITEMS`, `STATUS_COLORS`, `ACCOUNT_TYPE_LABELS`, `ACCOUNT_TYPE_ICONS`, `TRANSACTION_STATUS_LABELS`
- [ ] 2.10 Criar `src/shared/hooks/useDebounce.ts` — hook genérico de debounce para filtros de busca (300ms default)
- [ ] 2.11 Criar `src/shared/hooks/useFormatCurrency.ts` — hook que retorna valor formatado em R$
- [ ] 2.12 Criar `src/shared/types/api.ts` — interfaces `PagedResponse<T>` e `ProblemDetails` conforme techspec
- [ ] 2.13 Criar `src/app/router/routes.tsx` — definição de rotas com React Router v6: `/login`, `/` (redirect → `/dashboard`), `/dashboard`, `/transactions`, `/accounts`, `/categories`, `/admin`; usar `React.lazy()` para code splitting
- [ ] 2.14 Atualizar `src/App.tsx` para usar `RouterProvider` ou `BrowserRouter` com as rotas definidas
- [ ] 2.15 Criar componente `src/shared/components/ui/ConfirmationModal.tsx` — modal genérico de confirmação para ações destrutivas (cancelamento, inativação, import backup). Props: título, mensagem, onConfirm, onCancel, variant (danger/warning)
- [ ] 2.16 Testes: testar renderização do AppShell, navegação da Sidebar, formatters (formatCurrency, formatDate)

## Sequenciamento

- Bloqueado por: 1.0 (Scaffold)
- Desbloqueia: 3.0 (Auth)
- Paralelizável: Sim, com 4.0 (ajustes backend)

## Detalhes de Implementação

### Sidebar — Referência visual (do mockup `dashboard/index.html`)

```
┌────────────────────────────┐
│ 🔲 GestorFinanceiro        │  ← Logo + nome
│────────────────────────────│
│ 📊 Dashboard      ← ativo │  ← bg-primary/10 text-primary
│ 📋 Transações              │
│ 🏦 Contas                  │
│ 📂 Categorias              │
│────────────────────────────│
│ ⚙️ Configurações           │
│   👤 Admin                 │
│────────────────────────────│
│ 🟢 Sistema Online          │  ← footer com status
└────────────────────────────┘
```

Material Icons usados nos mockups:
- Dashboard: `dashboard`
- Transações: `receipt_long`
- Contas: `account_balance`
- Categorias: `category`
- Admin: `admin_panel_settings`
- Logo: `account_balance_wallet`

### Topbar — Estrutura

```
┌──────────────────────────────────────────────────────────┐
│  Visão Geral              🔔  │  Carlos Silva  │ 🚪     │
│                               │  Plano Familiar│ logout  │
└──────────────────────────────────────────────────────────┘
```

### PagedResponse e ProblemDetails

```typescript
// shared/types/api.ts
export interface PagedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    size: number;
    total: number;
    totalPages: number;
  };
}

export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance?: string;
}
```

### Formatters

```typescript
export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value);
}

export function formatDate(date: string | Date): string {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(date));
}
```

## Critérios de Sucesso

- Sidebar renderiza com todos os links de navegação e ícones conforme mockup
- Topbar exibe título dinâmico da página, avatar e botão de logout
- AppShell combina Sidebar + Topbar + conteúdo corretamente, sem scroll duplo
- Navegação entre rotas funciona via React Router (lazy loaded)
- `formatCurrency(1234.56)` retorna `"R$ 1.234,56"`
- `formatDate('2026-01-15')` retorna `"15/01/2026"`
- Componentes Shadcn/UI instalados e utilizáveis (Button, Card, Modal, etc.)
- Gráficos Recharts renderizam com dados de exemplo
- Testes dos formatters passam
- Layout visual fiel aos mockups em `screen-examples/dashboard/` e `screen-examples/gestao-contas/`
