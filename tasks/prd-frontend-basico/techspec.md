# Especificação Técnica — Frontend Básico (Fase 3)

## Resumo Executivo

A Fase 3 entrega a interface web do GestorFinanceiro em React + TypeScript + Vite, conectada à API REST da Fase 2. As decisões arquiteturais centrais são: (1) estrutura feature-based com features isoladas por domínio (auth, dashboard, accounts, categories, transactions, admin); (2) Shadcn/UI sobre Radix primitives + Tailwind CSS para componentes reutilizáveis e estilização, reproduzindo fielmente as telas de referência; (3) Zustand para estado global (auth, UI) e TanStack Query para cache, loading states e sincronização com a API; (4) Axios com interceptors para injeção automática de JWT e tratamento de 401; (5) sidebar fixa colapsável no mobile como padrão de navegação; (6) runtime config via `window.RUNTIME_ENV` para container único multi-ambiente conforme `rules/react-containers.md`.

A implementação exige a criação de novos endpoints de agregação na API (Fase 2.1) para alimentar o dashboard — a API atual possui apenas CRUDs, sem endpoints de resumo. O frontend é uma aplicação independente no diretório `frontend/` na raiz do monorepo, sem dependência de build do backend.

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌──────────────────────────────────────────────────────────────────────┐
│  frontend/ (React + Vite + TypeScript)                               │
│                                                                      │
│  src/                                                                │
│  ├─ app/                                                             │
│  │  ├─ providers/   (QueryClient, Auth, Theme, Toast)                │
│  │  └─ router/      (routes.tsx — React Router v6)                   │
│  │                                                                   │
│  ├─ shared/                                                          │
│  │  ├─ components/                                                   │
│  │  │  ├─ ui/       (Shadcn/UI: Button, Card, Input, Modal,         │
│  │  │  │             Select, Table, Tabs, Toggle, Badge, Toast)      │
│  │  │  ├─ layout/   (AppShell, Sidebar, Topbar, ProtectedRoute)     │
│  │  │  └─ charts/   (BarChart, DonutChart — wrappers Recharts)      │
│  │  ├─ hooks/       (useDebounce, useMediaQuery, useFormatCurrency)  │
│  │  ├─ utils/       (formatters, validators, constants)              │
│  │  ├─ services/    (apiClient.ts — Axios + interceptors)            │
│  │  ├─ config/      (runtimeConfig.ts — window.RUNTIME_ENV)         │
│  │  └─ types/       (api.ts, pagination.ts)                         │
│  │                                                                   │
│  └─ features/                                                        │
│     ├─ auth/        (login, logout, token management)                │
│     ├─ dashboard/   (KPI cards, gráficos, resumo mensal)            │
│     ├─ accounts/    (CRUD de contas)                                 │
│     ├─ categories/  (CRUD de categorias)                             │
│     ├─ transactions/(CRUD, filtros, paginação, detalhe)             │
│     └─ admin/       (gestão de usuários, backup)                    │
└──────────────────────────────────────────────────────────────────────┘
             │  HTTP/JSON (JWT Bearer)
             ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Backend API (.NET — Fase 2)                                         │
│  /api/v1/auth, /accounts, /categories, /transactions,               │
│  /users, /backup, /dashboard (NOVO)                                  │
└──────────────────────────────────────────────────────────────────────┘
```

**Fluxo de dados**: Componente de página → hook de feature (TanStack Query) → apiClient (Axios) → API REST → resposta cacheada → re-render reativo.

**Separação de responsabilidades**:
- `shared/` — componentes UI genéricos, utilitários e configuração. Não conhecem domínio.
- `features/*/` — cada feature é uma "ilha" com API calls, hooks, componentes, páginas e tipos próprios.
- `app/` — providers globais e roteamento. Orquestra features sem lógica de negócio.

---

## Design de Implementação

### Interfaces Principais

```typescript
// === shared/services/apiClient.ts ===
// Axios instance com interceptors de auth e error handling

import axios from 'axios';
import { API_URL } from '@/shared/config/runtimeConfig';

const apiClient = axios.create({
  baseURL: API_URL,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
});

// Request interceptor: injeta JWT
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Response interceptor: trata 401 → logout
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Tenta refresh; se falhar, logout
      const refreshed = await tryRefreshToken();
      if (!refreshed) useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  }
);
```

```typescript
// === features/auth/store/authStore.ts ===
// Zustand store para estado de autenticação

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserResponse | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<boolean>;
  setTokens: (access: string, refresh: string, user: UserResponse) => void;
}
```

```typescript
// === features/dashboard/hooks/useDashboard.ts ===
// TanStack Query hook para dados do dashboard

function useDashboardSummary(month: number, year: number) {
  return useQuery({
    queryKey: ['dashboard', 'summary', month, year],
    queryFn: () => getDashboardSummary(month, year),
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}

function useDashboardCharts(month: number, year: number) {
  return useQuery({
    queryKey: ['dashboard', 'charts', month, year],
    queryFn: () => getDashboardCharts(month, year),
    staleTime: 5 * 60 * 1000,
  });
}
```

### Modelos de Dados

Os tipos TypeScript espelham os DTOs da API (Fase 2):

```typescript
// === shared/types/api.ts ===

// Resposta paginada padrão da API (conforme rules/restful.md)
interface PagedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    size: number;
    total: number;
    totalPages: number;
  };
}

// Problem Details (RFC 9457) para erros
interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance?: string;
}
```

```typescript
// === features/accounts/types/account.ts ===

enum AccountType {
  Corrente = 1,
  Cartao = 2,
  Investimento = 3,
  Carteira = 4,
}

interface AccountResponse {
  id: string;
  name: string;
  type: AccountType;
  balance: number;
  allowNegativeBalance: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

interface CreateAccountRequest {
  name: string;
  type: AccountType;
  initialBalance: number;
  allowNegativeBalance: boolean;
  operationId?: string;
}
```

```typescript
// === features/transactions/types/transaction.ts ===

enum TransactionType { Debit = 1, Credit = 2 }
enum TransactionStatus { Paid = 1, Pending = 2, Cancelled = 3 }

interface TransactionResponse {
  id: string;
  accountId: string;
  categoryId: string;
  type: TransactionType;
  amount: number;
  description: string;
  competenceDate: string;
  dueDate: string | null;
  status: TransactionStatus;
  isAdjustment: boolean;
  originalTransactionId: string | null;
  hasAdjustment: boolean;
  installmentGroupId: string | null;
  installmentNumber: number | null;
  totalInstallments: number | null;
  isRecurrent: boolean;
  recurrenceTemplateId: string | null;
  transferGroupId: string | null;
  cancellationReason: string | null;
  cancelledBy: string | null;
  cancelledAt: string | null;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string | null;
}

interface CreateTransactionRequest {
  accountId: string;
  categoryId: string;
  type: TransactionType;
  amount: number;
  description: string;
  competenceDate: string;
  dueDate?: string;
  status: TransactionStatus;
  operationId?: string;
}

interface CreateInstallmentRequest {
  accountId: string;
  categoryId: string;
  type: TransactionType;
  totalAmount: number;
  installmentCount: number;
  description: string;
  firstCompetenceDate: string;
  firstDueDate?: string;
  operationId?: string;
}

interface CreateTransferRequest {
  sourceAccountId: string;
  destinationAccountId: string;
  categoryId: string;
  amount: number;
  description: string;
  competenceDate: string;
  operationId?: string;
}
```

```typescript
// === features/dashboard/types/dashboard.ts ===

interface DashboardSummary {
  totalBalance: number;
  monthlyIncome: number;
  monthlyExpenses: number;
  creditCardDebt: number;
}

interface MonthlyComparison {
  month: string; // "2026-01"
  income: number;
  expenses: number;
}

interface CategoryExpense {
  categoryId: string;
  categoryName: string;
  total: number;
  percentage: number;
}

interface DashboardCharts {
  revenueVsExpense: MonthlyComparison[]; // últimos 6 meses
  expenseByCategory: CategoryExpense[];  // mês selecionado
}
```

### Endpoints de API

#### Endpoints Existentes (Fase 2) — Consumidos pelo Frontend

| Método | Caminho | Descrição |
|--------|---------|-----------|
| `POST` | `/api/v1/auth/login` | Login (email + password → AuthResponse) |
| `POST` | `/api/v1/auth/refresh` | Refresh token |
| `POST` | `/api/v1/auth/logout` | Logout (revoga tokens) |
| `POST` | `/api/v1/auth/change-password` | Alterar senha |
| `GET` | `/api/v1/accounts` | Listar contas |
| `GET` | `/api/v1/accounts/{id}` | Obter conta |
| `POST` | `/api/v1/accounts` | Criar conta |
| `PUT` | `/api/v1/accounts/{id}` | Editar conta |
| `PATCH` | `/api/v1/accounts/{id}/status` | Ativar/inativar conta |
| `GET` | `/api/v1/categories` | Listar categorias |
| `POST` | `/api/v1/categories` | Criar categoria |
| `PUT` | `/api/v1/categories/{id}` | Editar categoria |
| `GET` | `/api/v1/transactions` | Listar transações (paginado + filtros) |
| `GET` | `/api/v1/transactions/{id}` | Obter transação |
| `POST` | `/api/v1/transactions` | Criar transação simples |
| `POST` | `/api/v1/transactions/installments` | Criar parcelamento |
| `POST` | `/api/v1/transactions/recurrences` | Criar recorrência |
| `POST` | `/api/v1/transactions/transfers` | Criar transferência |
| `POST` | `/api/v1/transactions/{id}/adjustments` | Ajustar transação |
| `POST` | `/api/v1/transactions/{id}/cancel` | Cancelar transação |
| `GET` | `/api/v1/transactions/{id}/history` | Histórico de auditoria |
| `GET` | `/api/v1/users` | Listar usuários (Admin) |
| `POST` | `/api/v1/users` | Criar usuário (Admin) |
| `PATCH` | `/api/v1/users/{id}/status` | Toggle status usuário (Admin) |
| `GET` | `/api/v1/backup/export` | Exportar backup JSON (Admin) |
| `POST` | `/api/v1/backup/import` | Importar backup JSON (Admin) |

#### Novos Endpoints Necessários (Fase 2.1 — Backend)

Estes endpoints devem ser implementados no backend para suportar o dashboard. A criação deles é dependência bloqueante para a feature de dashboard.

| Método | Caminho | Descrição | Request | Response |
|--------|---------|-----------|---------|----------|
| `GET` | `/api/v1/dashboard/summary?month=1&year=2026` | Cards de resumo (saldo total, receitas, despesas, dívida cartão) | Query params: `month`, `year` | `DashboardSummary` |
| `GET` | `/api/v1/dashboard/charts?month=1&year=2026` | Dados para gráficos (receita vs despesa 6 meses + despesa por categoria mês) | Query params: `month`, `year` | `DashboardCharts` |

**Implementação sugerida no backend**: Query handlers com SQL agregado direto no `FinanceiroDbContext` via LINQ. O `DashboardSummary` soma saldos das contas ativas e filtra transações `Paid` no mês de competência. O `DashboardCharts` agrega transações dos últimos 6 meses e agrupa despesas por categoria.

---

## Pontos de Integração

### API Backend (.NET — Fase 2)

- **Protocolo**: HTTP/JSON sobre REST
- **Autenticação**: JWT Bearer Token no header `Authorization: Bearer <token>`
- **Refresh**: Token refresh automático via interceptor Axios quando access token expira (401)
- **Erros**: Respostas em formato Problem Details (RFC 9457) mapeadas para toasts de erro
- **Paginação**: Offset-based com `_page` e `_size` como query params
- **CORS**: Backend deve permitir origem do frontend (`http://localhost:5173` em dev)
- **Timeouts**: 30s padrão, extendido para backup import (120s)

### Tratamento de Erros

```typescript
// Mapeamento de Problem Details → mensagens de UI
const ERROR_MESSAGES: Record<string, string> = {
  'AccountNameAlreadyExists': 'Já existe uma conta com este nome.',
  'InsufficientBalance': 'Saldo insuficiente para esta operação.',
  'InvalidCredentials': 'Credenciais inválidas.',
  'CategoryNameAlreadyExists': 'Já existe uma categoria com este nome.',
  // ... demais mapeamentos
};

function handleApiError(error: AxiosError<ProblemDetails>): string {
  const problem = error.response?.data;
  if (problem?.type) {
    const key = problem.type.split('/').pop();
    return ERROR_MESSAGES[key] ?? problem.detail ?? 'Erro inesperado.';
  }
  return 'Erro de conexão. Tente novamente.';
}
```

---

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
|---|---|---|---|
| Backend API (Fase 2) | Novos endpoints | Dashboard exige 2 novos endpoints de agregação (`/dashboard/summary`, `/dashboard/charts`). Risco baixo. | Implementar Query handlers + Controller no backend antes do dashboard |
| `AccountResponse` DTO | Correção de DTO | DTO atual omite `Type` e `AllowNegativeBalance` que a UI precisa. Risco baixo. | Adicionar campos faltantes no DTO do backend |
| `ListTransactionsByAccountQuery` | Extensão de query | Query atual não suporta filtros (conta, categoria, status, período) nem paginação. Risco médio. | Substituir por `ListTransactionsQuery` com filtros e paginação |
| `ListAccountsQuery` / `ListCategoriesQuery` | Extensão de query | Queries atuais não suportam filtro de status/tipo. Risco baixo. | Adicionar parâmetros opcionais de filtro |
| CORS do Backend | Configuração | Frontend em porta diferente exige CORS configurado. Risco baixo. | Adicionar configuração CORS no `Program.cs` do backend |
| Monorepo | Novo diretório | Novo diretório `frontend/` na raiz. Sem impacto no backend. Risco zero. | Criar estrutura do projeto React |

---

## Abordagem de Testes

### Testes Unitários

**Framework**: Vitest + React Testing Library (RTL)  
**Cobertura mínima**: 70% das features críticas (auth, transactions, dashboard)

**Componentes a testar prioritariamente**:
- `LoginForm` — validação inline, submit, tratamento de erro
- `TransactionForm` — alternância de abas, validação por tipo, preview de parcelas
- `DashboardSummaryCards` — renderização com dados, estados de loading/erro
- `TransactionFilters` — aplicação/remoção de filtros, sincronização com URL
- `AccountCard` — toggle de status com confirmação
- `ConfirmationModal` — ações destrutivas (cancelamento, import backup)

**Hooks a testar**:
- `useAuth` — login, logout, refresh, estados de autenticação
- `useDashboard` — cache, revalidação, tratamento de erro
- `useTransactionFilters` — serialização/deserialização de query params

**Mocking**:
- MSW (Mock Service Worker) para simular API em testes de integração de componentes
- Handlers por feature: `features/*/test/handlers.ts`

### Testes de Integração

**Foco**: Fluxos completos de usuário com API mockada (MSW)
- Login → Dashboard → Nova Transação → Listagem atualizada
- CRUD completo de Contas (criar, editar, inativar)
- Filtros e paginação de transações
- Cancelamento e ajuste de transação
- Backup export/import (fluxo admin)

**Setup**: Wrapper de teste com todos os providers (QueryClient, Zustand, Router)

```typescript
// shared/test/renderWithProviders.tsx
function renderWithProviders(ui: ReactElement, options?: RenderOptions) {
  return render(ui, {
    wrapper: ({ children }) => (
      <QueryClientProvider client={testQueryClient}>
        <MemoryRouter>{children}</MemoryRouter>
      </QueryClientProvider>
    ),
    ...options,
  });
}
```

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

1. **Scaffold do projeto + infraestrutura** (Semana 1)
   - Criar projeto Vite + React + TypeScript em `frontend/`
   - Configurar Tailwind CSS, Shadcn/UI, path aliases
   - Configurar `runtimeConfig.ts`, Axios client, Zustand store base
   - Setup de testes: Vitest, RTL, MSW
   - Componentes de layout: `AppShell`, `Sidebar`, `Topbar`
   - Dependência: nenhuma

2. **Autenticação (feature auth)** (Semana 1–2)
   - Tela de login com validação (react-hook-form + zod)
   - Zustand auth store (tokens, user, login/logout)
   - Axios interceptors (JWT inject, 401 → refresh/logout)
   - `ProtectedRoute` wrapper
   - Fluxo completo: login → redirect dashboard → logout
   - Dependência: F1 scaffold

3. **Dashboard** (Semana 2–3)
   - **Backend (Fase 2.1)**: Implementar endpoints `/dashboard/summary` e `/dashboard/charts`
   - KPI cards (saldo total, receitas, despesas, dívida cartão)
   - Navegador de mês/ano
   - Gráfico de barras (Recharts — receita vs despesa 6 meses)
   - Gráfico donut (Recharts — despesas por categoria)
   - Tabela de transações recentes
   - Dependência: F2 auth + endpoints de dashboard

4. **CRUD de Contas** (Semana 3)
   - Listagem em cards (grid responsivo)
   - Formulário de criação/edição (modal ou página)
   - Toggle ativar/inativar com confirmação
   - Filtro por tipo de conta
   - Footer com patrimônio consolidado
   - Dependência: F2 auth

5. **CRUD de Categorias** (Semana 3–4)
   - Listagem com filtro por tipo (Receita/Despesa)
   - Formulário de criação (nome + tipo)
   - Edição (apenas nome)
   - Indicação visual do tipo
   - Dependência: F2 auth

6. **CRUD de Transações** (Semana 4–5)
   - Listagem com tabela paginada e filtros (conta, categoria, tipo, status, período)
   - Sincronização de filtros com URL (query params)
   - Formulário de criação com abas (Simples, Parcelada, Recorrente, Transferência)
   - Preview de parcelas antes de confirmar
   - Ações: cancelar (com motivo), ajustar (novo valor)
   - Detalhe da transação com histórico de auditoria
   - Dependência: F4 contas + F5 categorias (selects nos formulários)

7. **Painel Admin** (Semana 5–6)
   - Guard de rota (apenas role Admin)
   - Gestão de usuários: listagem, criação, toggle status
   - Backup: export (download JSON), import (upload com confirmação)
   - Dependência: F2 auth

8. **Polimento e Testes** (Semana 6)
   - Skeleton loaders em todas as telas
   - Toasts de feedback (react-hot-toast)
   - Estados vazios (empty states)
   - Testes unitários e de integração
   - Acessibilidade: labels, navegação por teclado, contraste AA
   - Dependência: Todas as features implementadas

### Dependências Técnicas

| Dependência | Tipo | Status | Ação |
|---|---|---|---|
| Endpoints de agregação (`/dashboard/*`) | Backend bloqueante para dashboard | **Não implementado** | Criar task na Fase 2.1 |
| `AccountResponse` com `Type` e `AllowNegativeBalance` | Backend bloqueante para contas | **Campos faltantes** | Corrigir DTO |
| `ListTransactionsQuery` com filtros e paginação | Backend bloqueante para transações | **Não implementado** | Evoluir query existente |
| Filtros em `ListAccountsQuery` e `ListCategoriesQuery` | Backend desejável | **Não implementado** | Implementação do filtro client-side como fallback |
| CORS configurado no backend | Backend bloqueante | **Não implementado** | Configurar no `Program.cs` |
| Backend da Fase 2 funcional (controllers) | Backend bloqueante | **Não implementado** | Implementar controllers da Fase 2 primeiro |

---

## Monitoramento e Observabilidade

### Telemetria (Produção)

Conforme `rules/react-logging.md`, usar OpenTelemetry para tracing em produção:

- **Service name**: `frontend`
- **Instrumentação automática**: fetch, document-load, user-interaction
- **Propagação**: W3C Trace Context nos headers para APIs do GestorFinanceiro
- **Exporter**: OTLP via HTTP (endpoint configurável via `window.RUNTIME_ENV`)
- **Habilitação**: apenas em `import.meta.env.PROD`

### Logging

- Console logs em desenvolvimento apenas (`import.meta.env.DEV`)
- Erros de API logados no interceptor do Axios com contexto (URL, status, operationId)
- Erros JavaScript não tratados capturados via `window.onerror` (produção)

### Métricas de UX (Futuro)

- Tempo de carregamento do dashboard
- Taxa de erros de API por endpoint
- Tempo médio de preenchimento de formulários

---

## Considerações Técnicas

### Decisões Principais

| Decisão | Escolha | Justificativa | Alternativas Rejeitadas |
|---|---|---|---|
| **UI Library** | Shadcn/UI + Tailwind CSS | Componentes headless (Radix) com controle total de estilo via Tailwind. Boa DX, zero runtime overhead. Mantém fidelidade aos mockups que já usam Tailwind. | Tailwind puro (mais código boilerplate para modals/selects/tabs), Mantine (estilo próprio iria conflitar com mockups Tailwind) |
| **State Management** | Zustand (global) + TanStack Query (server state) | Zustand é mínimo (~1KB), sem boilerplate. TanStack Query gerencia cache, revalidação, loading/error states automaticamente. | Context API (verbose para auth global), Redux (overkill para app familiar) |
| **Charts** | Recharts | Wrapper React declarativo sobre D3, leve, API simples. Suporta BarChart e PieChart nativamente. | Chart.js (API imperativa, menos React-idiomatic), Nivo (mais pesado), CSS puro (limitado para interatividade) |
| **Forms** | react-hook-form + zod | Validação inline com boa performance (uncontrolled). Zod fornece inferência de tipos TypeScript. | Formik (mais pesado, menos tipado), validação manual (propenso a erros) |
| **HTTP Client** | Axios | Interceptors nativos para JWT e error handling. Suporte a cancelamento e timeout. | fetch nativo (sem interceptors, mais boilerplate para retry/refresh) |
| **Roteamento** | React Router v6 | Padrão de mercado, suporta nested routes e route guards. | TanStack Router (mais novo, menos documentação) |
| **Navegação** | Sidebar fixa w-64 | Fiel aos mockups, boa área de navegação para 6-8 itens. Colapsável no mobile. | Navbar superior (menos espaço para itens futuros) |
| **Runtime Config** | `window.RUNTIME_ENV` | Container único para dev/homolog/prod, conforme `rules/react-containers.md`. Sem rebuild por ambiente. | `import.meta.env` (exige rebuild por ambiente) |
| **Ícones** | Material Icons (Google Fonts) | Já usado em todos os mockups. Consistência visual garantida. | Lucide React (sugerido no UX Guide, mas mockups já usam Material Icons) |

### Riscos Conhecidos

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| **API da Fase 2 incompleta** (controllers não implementados, `Program.cs` está vazio) | Alta | Bloqueante | Desenvolver com MSW mocks; priorizar implementação dos controllers no backend |
| **Endpoints de dashboard inexistentes** | Certa | Bloqueante para dashboard | Mock durante desenvolvimento; implementar como Fase 2.1 |
| **Performance com muitas transações** | Média | Médio | Paginação obrigatória, TanStack Query com `staleTime`, virtualização de lista se necessário |
| **Token JWT storage** | Baixa | Alto (segurança) | Armazenar tokens no Zustand (memória) e `localStorage` para persistência entre tabs. Avaliar httpOnly cookies se backend suportar. |
| **CORS não configurado** | Certa (backend atual) | Bloqueante | Configurar CORS no backend; em dev usar proxy do Vite (`vite.config.ts → server.proxy`) |

### Requisitos Especiais

#### Performance
- Dashboard deve carregar em < 2s com cache ativo (TanStack Query `staleTime: 5min`)
- Listagens com paginação server-side (máximo 20 itens/página)
- Skeleton loaders para evitar layout shift durante loading
- Lazy loading de rotas via `React.lazy()` para features fora do bundle principal

#### Segurança
- Tokens JWT armazenados em memória (Zustand) + `localStorage` para persistência
- Refresh automático antes da expiração (interceptor)
- Logout revoga tokens no backend e limpa estado local
- Rotas admin protegidas por role guard (verificação client-side + backend)
- Sanitização de inputs em formulários (zod); backend faz validação final

#### Acessibilidade (WCAG AA)
- Labels em todos os campos de formulário (`<label htmlFor>`)
- Navegação por teclado funcional (focus management em modals)
- Contraste mínimo 4.5:1 para texto, 3:1 para elementos interativos
- `aria-label` em ícones informativos
- Roles semânticos: `role="navigation"`, `role="main"`, `role="dialog"`

### Conformidade com Padrões

- [x] Segue `rules/react-project-structure.md` — estrutura feature-based com `shared/` e `features/`
- [x] Segue `rules/react-coding-standards.md` — nomenclatura inglês, PascalCase componentes, camelCase hooks/utils
- [x] Segue `rules/react-testing.md` — Vitest + RTL + MSW, padrão AAA
- [x] Segue `rules/react-containers.md` — `window.RUNTIME_ENV` para config runtime, Dockerfile multi-stage com Nginx
- [x] Segue `rules/react-logging.md` — OpenTelemetry em produção, service name `frontend`
- [x] Segue `rules/restful.md` — consome API com paginação `_page`/`_size`, erros RFC 9457
- [x] Segue `rules/git-commit.md` — commits em português, formato convencional

---

## Estrutura de Pastas Completa

```text
frontend/
├── public/
│   ├── runtime-env.template.js
│   └── favicon.svg
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   ├── index.css                    # Tailwind directives + custom tokens
│   │
│   ├── app/
│   │   ├── providers/
│   │   │   ├── AppProviders.tsx      # Compõe todos os providers
│   │   │   └── QueryProvider.tsx
│   │   └── router/
│   │       └── routes.tsx           # Definição de rotas com lazy loading
│   │
│   ├── shared/
│   │   ├── components/
│   │   │   ├── ui/                  # Shadcn/UI components customizados
│   │   │   │   ├── Button.tsx
│   │   │   │   ├── Card.tsx
│   │   │   │   ├── Input.tsx
│   │   │   │   ├── Select.tsx
│   │   │   │   ├── Modal.tsx
│   │   │   │   ├── Table.tsx
│   │   │   │   ├── Tabs.tsx
│   │   │   │   ├── Toggle.tsx
│   │   │   │   ├── Badge.tsx
│   │   │   │   ├── Skeleton.tsx
│   │   │   │   └── Toast.tsx
│   │   │   ├── layout/
│   │   │   │   ├── AppShell.tsx     # Container autenticado (sidebar + topbar + main)
│   │   │   │   ├── Sidebar.tsx
│   │   │   │   ├── Topbar.tsx
│   │   │   │   └── ProtectedRoute.tsx
│   │   │   └── charts/
│   │   │       ├── BarChartWidget.tsx
│   │   │       └── DonutChartWidget.tsx
│   │   │
│   │   ├── hooks/
│   │   │   ├── useDebounce.ts
│   │   │   ├── useMediaQuery.ts
│   │   │   └── useFormatCurrency.ts
│   │   │
│   │   ├── utils/
│   │   │   ├── formatters.ts        # formatCurrency, formatDate, formatStatus
│   │   │   ├── validators.ts
│   │   │   └── constants.ts         # NAV_ITEMS, STATUS_COLORS, ACCOUNT_TYPE_LABELS
│   │   │
│   │   ├── services/
│   │   │   └── apiClient.ts         # Axios instance + interceptors
│   │   │
│   │   ├── config/
│   │   │   └── runtimeConfig.ts     # window.RUNTIME_ENV
│   │   │
│   │   ├── types/
│   │   │   ├── api.ts               # PagedResponse, ProblemDetails
│   │   │   └── index.ts
│   │   │
│   │   └── test/
│   │       ├── setup.ts             # Vitest global setup
│   │       ├── renderWithProviders.tsx
│   │       └── mocks/
│   │           ├── handlers.ts
│   │           └── server.ts
│   │
│   └── features/
│       ├── auth/
│       │   ├── api/
│       │   │   └── authApi.ts
│       │   ├── components/
│       │   │   └── LoginForm.tsx
│       │   ├── hooks/
│       │   │   └── useAuth.ts
│       │   ├── pages/
│       │   │   └── LoginPage.tsx
│       │   ├── store/
│       │   │   └── authStore.ts
│       │   ├── types/
│       │   │   └── auth.ts
│       │   └── index.ts
│       │
│       ├── dashboard/
│       │   ├── api/
│       │   │   └── dashboardApi.ts
│       │   ├── components/
│       │   │   ├── SummaryCards.tsx
│       │   │   ├── MonthNavigator.tsx
│       │   │   ├── RevenueExpenseChart.tsx
│       │   │   ├── CategoryExpenseChart.tsx
│       │   │   └── RecentTransactions.tsx
│       │   ├── hooks/
│       │   │   └── useDashboard.ts
│       │   ├── pages/
│       │   │   └── DashboardPage.tsx
│       │   ├── types/
│       │   │   └── dashboard.ts
│       │   └── index.ts
│       │
│       ├── accounts/
│       │   ├── api/
│       │   │   └── accountsApi.ts
│       │   ├── components/
│       │   │   ├── AccountCard.tsx
│       │   │   ├── AccountGrid.tsx
│       │   │   ├── AccountForm.tsx
│       │   │   └── AccountSummaryFooter.tsx
│       │   ├── hooks/
│       │   │   └── useAccounts.ts
│       │   ├── pages/
│       │   │   └── AccountsPage.tsx
│       │   ├── types/
│       │   │   └── account.ts
│       │   └── index.ts
│       │
│       ├── categories/
│       │   ├── api/
│       │   │   └── categoriesApi.ts
│       │   ├── components/
│       │   │   ├── CategoryList.tsx
│       │   │   ├── CategoryForm.tsx
│       │   │   └── CategoryFilter.tsx
│       │   ├── hooks/
│       │   │   └── useCategories.ts
│       │   ├── pages/
│       │   │   └── CategoriesPage.tsx
│       │   ├── types/
│       │   │   └── category.ts
│       │   └── index.ts
│       │
│       ├── transactions/
│       │   ├── api/
│       │   │   └── transactionsApi.ts
│       │   ├── components/
│       │   │   ├── TransactionTable.tsx
│       │   │   ├── TransactionFilters.tsx
│       │   │   ├── TransactionForm.tsx
│       │   │   ├── TransactionDetail.tsx
│       │   │   ├── InstallmentPreview.tsx
│       │   │   ├── CancelModal.tsx
│       │   │   └── AdjustModal.tsx
│       │   ├── hooks/
│       │   │   ├── useTransactions.ts
│       │   │   └── useTransactionFilters.ts
│       │   ├── pages/
│       │   │   ├── TransactionsPage.tsx
│       │   │   └── TransactionDetailPage.tsx
│       │   ├── types/
│       │   │   └── transaction.ts
│       │   └── index.ts
│       │
│       └── admin/
│           ├── api/
│           │   ├── usersApi.ts
│           │   └── backupApi.ts
│           ├── components/
│           │   ├── UserTable.tsx
│           │   ├── UserForm.tsx
│           │   ├── BackupExport.tsx
│           │   └── BackupImport.tsx
│           ├── hooks/
│           │   ├── useUsers.ts
│           │   └── useBackup.ts
│           ├── pages/
│           │   └── AdminPage.tsx
│           ├── types/
│           │   └── admin.ts
│           └── index.ts
│
├── docker/
│   └── 40-runtime-env.sh
├── Dockerfile
├── index.html
├── package.json
├── tsconfig.json
├── tsconfig.node.json
├── vite.config.ts
├── vitest.config.ts
├── tailwind.config.ts
├── postcss.config.js
├── components.json               # Shadcn/UI config
└── .eslintrc.cjs
```

---

## Paleta de Cores e Tokens Visuais

Extraída dos mockups e padronizada como tokens Tailwind customizados:

```typescript
// tailwind.config.ts (extensão de tema)
theme: {
  extend: {
    colors: {
      primary: { DEFAULT: '#137fec', dark: '#0e62b6' },
      success: '#10b981',
      danger: '#ef4444',
      warning: '#f59e0b',
      background: { light: '#f6f7f8', dark: '#101922' },
      surface: { light: '#ffffff', dark: '#182430' },
    },
    fontFamily: {
      sans: ['Inter', 'system-ui', 'sans-serif'],
    },
    borderRadius: {
      DEFAULT: '0.25rem',
      lg: '0.5rem',
      xl: '0.75rem',
      '2xl': '1rem',
    },
  },
}
```

| Elemento | Token | Uso |
|---|---|---|
| Botão primário | `bg-primary hover:bg-primary-dark` | CTAs principais |
| Receita/Credit | `text-success` / `bg-success/10` | Valores positivos, badges |
| Despesa/Debit | `text-danger` / `bg-danger/10` | Valores negativos, alertas |
| Aviso | `text-warning` / `bg-warning/10` | Cartão de crédito, pendências |
| Cards | `bg-surface-light rounded-xl shadow-sm border` | Cards e painéis |
| Nav ativo | `bg-primary/10 text-primary rounded-lg` | Item da sidebar selecionado |
| Status Pago | `bg-green-100 text-green-800 rounded-full` | Badge de status |
| Status Pendente | `bg-yellow-100 text-yellow-800 rounded-full` | Badge de status |
| Status Cancelado | `bg-gray-100 text-gray-800 rounded-full line-through` | Badge de status |

---

## Formatação e Localização (pt-BR)

```typescript
// shared/utils/formatters.ts

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

export function formatCompetenceMonth(month: number, year: number): string {
  const date = new Date(year, month - 1);
  return new Intl.DateTimeFormat('pt-BR', {
    month: 'long',
    year: 'numeric',
  }).format(date);
}
```
