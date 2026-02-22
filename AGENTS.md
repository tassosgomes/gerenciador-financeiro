# AGENTS.md — Guia de Contexto do Projeto GestorFinanceiro

> Este arquivo serve como referência rápida para LLMs e agentes de IA entenderem o projeto sem precisar fazer exploratórios extensivos a cada sessão.

---

## 1. Visão Geral do Projeto

**GestorFinanceiro** é um sistema de gestão financeira pessoal/familiar **self-hosted**, voltado para famílias ou indivíduos que desejam total controle e privacidade sobre seus dados financeiros, sem custos de assinatura.

### Propósito
- Controlar múltiplas contas bancárias (corrente, carteira, investimento, **cartão de crédito**)
- Registrar receitas e despesas com categorização
- Gerenciar transações avulsas, parceladas, recorrentes e transferências entre contas
- Controlar faturas de cartão de crédito (fechamento, vencimento, pagamento)
- Visualizar dashboard com métricas, gráficos e resumo financeiro mensal
- Gerenciar múltiplos usuários com controle de acesso (Admin/Membro)

### Stack Tecnológica
| Camada     | Tecnologia                                                                 |
|------------|---------------------------------------------------------------------------|
| Backend    | .NET 8, C#, ASP.NET Core Web API                                         |
| Frontend   | React 18, TypeScript, Vite, TailwindCSS                                  |
| Banco      | PostgreSQL 15 (via Docker)                                                |
| ORM        | Entity Framework Core + Npgsql                                           |
| Auth       | JWT (access + refresh token), BCrypt para hash de senha                   |
| UI Kit     | shadcn/ui (Radix UI + Tailwind), Recharts, Lucide icons                  |
| State      | Zustand (auth, app), TanStack React Query (server state)                  |
| Forms      | React Hook Form + Zod (validação)                                         |
| HTTP       | Axios (apiClient com interceptor de refresh token)                        |
| Testes BE  | xUnit, NSubstitute (unit), Testcontainers (integration)                   |
| Testes FE  | Vitest, Testing Library, MSW (mock service worker)                        |
| Deploy     | Docker Compose, multi-stage build, Nginx (frontend)                       |

---

## 2. Estrutura do Projeto

```
gerenciador-financeiro/
├── backend/                          # Solução .NET 8 (Clean Architecture em camadas)
│   ├── GestorFinanceiro.Financeiro.sln
│   ├── 1-Services/                   # Camada de API (Controllers, Middleware, Startup)
│   │   └── GestorFinanceiro.Financeiro.API/
│   ├── 2-Application/                # Camada de Aplicação (Commands, Queries, DTOs)
│   │   └── GestorFinanceiro.Financeiro.Application/
│   ├── 3-Domain/                     # Camada de Domínio (Entities, Enums, Interfaces, Services)
│   │   └── GestorFinanceiro.Financeiro.Domain/
│   ├── 4-Infra/                      # Camada de Infraestrutura (EF Core, Repos, Auth, Migrations)
│   │   └── GestorFinanceiro.Financeiro.Infra/
│   └── 5-Tests/                      # Testes (Unit, Integration, HttpIntegration, E2E)
│       ├── GestorFinanceiro.Financeiro.UnitTests/
│       ├── GestorFinanceiro.Financeiro.IntegrationTests/
│       ├── GestorFinanceiro.Financeiro.HttpIntegrationTests/
│       └── GestorFinanceiro.Financeiro.End2EndTests/
├── frontend/                         # Aplicação React + Vite
│   └── src/
│       ├── app/                      # Providers e Router
│       ├── features/                 # Módulos de feature (feature-based architecture)
│       │   ├── accounts/             # Contas bancárias e cartões de crédito
│       │   ├── admin/                # Administração de usuários e sistema
│       │   ├── auth/                 # Login, autenticação, store de auth
│       │   ├── categories/           # Categorias de receita/despesa
│       │   ├── dashboard/            # Dashboard com gráficos e resumo
│       │   └── transactions/         # Transações, parcelas, transferências
│       └── shared/                   # Código compartilhado
│           ├── components/           # UI (shadcn), Layout, Charts
│           ├── config/               # Runtime config (API_URL)
│           ├── hooks/                # Hooks utilitários
│           ├── lib/                  # Utils do shadcn (cn)
│           ├── services/             # apiClient (Axios)
│           ├── store/                # useAppStore (sidebar state)
│           ├── test/                 # Setup vitest, MSW mocks
│           ├── types/                # Tipos globais (PagedResponse, ProblemDetails)
│           └── utils/                # Formatadores, constantes, mensagens de erro
├── scripts/debug/                    # Scripts para desenvolvimento local
├── tasks/                            # PRDs, tasks e reviews de implementação
├── rules/                            # Regras e padrões de código (dotnet, react, git, etc.)
├── templates/                        # Templates de PRD, task, techspec, bug report
├── bugs/                             # Relatórios de bugs exploratórios
└── draft/                            # Rascunhos e análises de features futuras
```

---

## 3. Backend — Arquitetura e Padrões

### Arquitetura em Camadas (Clean Architecture)
O backend segue uma arquitetura em 4 camadas com dependência unidirecional:

```
API → Application → Domain ← Infra
```

- **Domain** é o core, sem dependências externas
- **Application** conhece apenas Domain
- **Infra** implementa interfaces do Domain
- **API** orquestra tudo via DI

### 3.1 Camada de Domínio (`3-Domain`)

**Entidades** (`Entity/`):
| Entidade             | Descrição                                                          |
|---------------------|--------------------------------------------------------------------|
| `BaseEntity`        | Classe base: Id (Guid), CreatedBy, CreatedAt, UpdatedBy, UpdatedAt |
| `Account`           | Conta bancária. Tipos: Corrente, Cartão, Investimento, Carteira    |
| `CreditCardDetails` | Value Object do cartão: limite, fechamento, vencimento, conta débito |
| `Transaction`       | Transação financeira (débito/crédito). Suporta parcelas, recorrência, ajuste, cancelamento, transferência |
| `Category`          | Categoria de receita ou despesa. Pode ser do sistema (imutável) ou do usuário |
| `User`              | Usuário com email, hash de senha, role (Admin/Member)              |
| `RecurrenceTemplate`| Template para geração automática de transações recorrentes         |
| `RefreshToken`      | Refresh token (hash armazenado) vinculado ao usuário               |
| `AuditLog`          | Log de auditoria de ações do sistema                               |
| `OperationLog`      | Log de operações para idempotência                                 |

**Enums** (`Enum/`):
| Enum                | Valores                                      |
|---------------------|----------------------------------------------|
| `AccountType`       | Corrente=1, Cartao=2, Investimento=3, Carteira=4 |
| `TransactionType`   | Debit=1, Credit=2                            |
| `TransactionStatus` | Paid=1, Pending=2, Cancelled=3               |
| `CategoryType`      | Receita=1, Despesa=2                         |
| `UserRole`          | Admin=1, Member=2                            |

**Domain Services** (`Service/`):
- `CreditCardDomainService` — Lógica de fechamento/vencimento de cartão
- `InstallmentDomainService` — Criação e cálculo de parcelas
- `RecurrenceDomainService` — Geração de transações recorrentes
- `TransactionDomainService` — Regras de ajuste, cancelamento e pagamento
- `TransferDomainService` — Transferência entre contas (gera par de transações)

**Interfaces** (`Interface/`): Contratos de repositórios seguindo Repository Pattern
- `IRepository<T>` (base), `IAccountRepository`, `ITransactionRepository`, `ICategoryRepository`, etc.
- `IUnitOfWork` para transações de banco de dados

**Exceptions** (`Exception/`): Domain exceptions fortemente tipadas (ex: `InsufficientBalanceException`, `CreditLimitExceededException`, `InvalidTransactionAmountException`). Todas herdam de `DomainException`.

### 3.2 Camada de Aplicação (`2-Application`)

Usa padrão **CQRS simplificado** com `ICommand<T>` / `IQuery<T>` e handlers correspondentes, orquestrados por `IDispatcher`.

**Commands** (`Commands/`): Organizado por feature
- `Account/` — Create, Update, Activate, Deactivate (com Validators)
- `Transaction/` — Create, Cancel, Adjust, MarkAsPaid
- `Transfer/` — Create, Cancel
- `Installment/` — Create, Cancel, CancelGroup, AdjustGroup
- `Invoice/` — PayInvoice
- `Recurrence/` — Create, Deactivate, Generate
- `Auth/` — Login, Refresh, Logout
- `Backup/` — Export, Import
- `Category/` — Create, Update
- `User/` — Create, Update, ChangePassword
- `System/` — Health, Info

**Queries** (`Queries/`): Organizado por feature
- `Account/`, `Transaction/`, `Category/`, `Dashboard/`, `Audit/`, `Backup/`, `Invoice/`, `User/`

**DTOs** (`Dtos/`): Response DTOs para serialização (ex: `AccountResponse`, `TransactionResponse`, `DashboardSummaryResponse`)

**Common** (`Common/`): Infraestrutura da aplicação
- `IDispatcher` / `Dispatcher` — Resolve e executa command/query handlers via DI
- `PagedResult<T>`, `PaginationQuery`, `PaginationMetadata` — Paginação
- `IStartupTask` — Interface para tarefas de inicialização
- `ApplicationServiceExtensions` — Registro de todos os handlers no DI

### 3.3 Camada de Infraestrutura (`4-Infra`)

- **Context**: `FinanceiroDbContext` (EF Core) com `FinanceiroDbContextFactory` (para migrations)
- **Config**: Configurações de mapeamento EF Core (Fluent API) para cada entidade
- **Repository**: Implementações concretas dos repositórios do Domain
- **Auth**: `PasswordHasher` (BCrypt), `TokenService` (JWT), `JwtSettings`
- **Audit**: `AuditService` — registro de logs de auditoria
- **Migrations**: EF Core migrations (PostgreSQL)
- **StartupTasks**:
  - `MigrateDatabaseStartupTask` — Aplica migrations automaticamente no startup
  - `SeedAdminUserStartupTask` — Cria usuário admin inicial
  - `SeedInvoicePaymentCategoryStartupTask` — Cria categoria "Pagamento de Fatura"
  - `RecurrenceMaintenanceWorker` — Background service que gera transações recorrentes
- **DependencyInjection**: `ServiceCollectionExtensions.AddInfrastructure()` — registra todos os serviços

### 3.4 Camada de API (`1-Services`)

**Controllers** (`Controllers/`):
| Controller               | Rota base                  | Responsabilidade                              |
|--------------------------|---------------------------|-----------------------------------------------|
| `AuthController`         | `api/v1/auth`             | Login, refresh token, logout                  |
| `AccountsController`     | `api/v1/accounts`         | CRUD de contas + cartões de crédito           |
| `TransactionsController` | `api/v1/transactions`     | Transações, parcelas, recorrências, transferências |
| `CategoriesController`   | `api/v1/categories`       | CRUD de categorias                            |
| `DashboardController`    | `api/v1/dashboard`        | Resumo e gráficos                             |
| `InvoicesController`     | `api/v1/invoices`         | Listar e pagar faturas de cartão              |
| `UsersController`        | `api/v1/users`            | Gestão de usuários (Admin)                    |
| `AuditController`        | `api/v1/audit`            | Logs de auditoria                             |
| `BackupController`       | `api/v1/backup`           | Export/Import de dados                        |
| `SystemController`       | `api/v1/system`           | Health check, info do sistema                 |

**Requests** (`Controllers/Requests/`): DTOs de entrada para cada endpoint

**Middleware**:
- `GlobalExceptionHandler` — Converte DomainExceptions em ProblemDetails (RFC 7807)

**Filters**:
- `ValidationActionFilter` — Valida commands via FluentValidation antes de chegar ao handler

**Extensions**:
- `ClaimsPrincipalExtensions` — Extrai userId dos claims JWT

**Autenticação**: JWT Bearer com refresh token. Todos os endpoints requerem `[Authorize]` exceto login/refresh.

---

## 4. Frontend — Arquitetura e Padrões

### Feature-Based Architecture
Cada feature é um módulo auto-contido com a estrutura:
```
features/<feature>/
├── api/           # Chamadas à API (funções com apiClient)
├── components/    # Componentes React da feature
├── hooks/         # Custom hooks (React Query mutations/queries)
├── pages/         # Páginas da feature (lazy-loaded)
├── schemas/       # Schemas Zod de validação de formulários
├── test/          # Testes da feature
├── types/         # Tipos TypeScript da feature
└── index.ts       # Barrel export
```

### Features Existentes

| Feature        | Página(s)                          | Descrição                                        |
|---------------|-----------------------------------|--------------------------------------------------|
| `auth`        | `/login`                          | Login com email/senha                            |
| `dashboard`   | `/dashboard`                      | Dashboard com cards de resumo, gráficos de receita/despesa e por categoria |
| `accounts`    | `/accounts`                       | Grid de contas, formulário de criação/edição, drawer de fatura de cartão |
| `transactions`| `/transactions`, `/transactions/:id`| Tabela paginada com filtros, formulário de transação, detalhe com timeline, modais de ajuste/cancelamento |
| `categories`  | `/categories`                     | Listagem e gestão de categorias                  |
| `admin`       | `/admin` (rota protegida Admin)   | Gestão de usuários e configurações do sistema    |

### Rotas (react-router-dom v6)
- `/login` — Pública
- `/` → redireciona para `/dashboard`
- `/dashboard`, `/transactions`, `/transactions/:id`, `/accounts`, `/categories` — Protegidas (ProtectedRoute)
- `/admin` — Protegida + AdminRoute (role Admin)
- `/*` — Redireciona para `/dashboard`

### Componentes Compartilhados (`shared/`)

**UI** (`components/ui/`): Componentes base do shadcn/ui
- `button`, `card`, `input`, `select`, `dialog`, `sheet`, `table`, `tabs`, `badge`, `skeleton`, `switch`, `progress`, `sonner` (toasts)
- `currency-input` — Input monetário formatado
- `ConfirmationModal` — Modal de confirmação genérico
- `EmptyState` — Componente de estado vazio
- `ErrorBoundary` — Captura de erros React
- `grouped-select-options` — Select com opções agrupadas

**Layout** (`components/layout/`):
- `AppShell` — Layout principal com sidebar + topbar + outlet
- `Sidebar` — Menu lateral com navegação
- `Topbar` — Barra superior com nome do usuário e logout
- `ProtectedRoute` — Guarda de rota autenticada
- `AdminRoute` — Guarda de rota para Admin

**Charts** (`components/charts/`): `BarChartWidget`, `DonutChartWidget` (wrappers do Recharts)

### Gerenciamento de Estado
- **Auth Store** (`features/auth/store/authStore.ts`): Zustand com persistência em localStorage. Gerencia accessToken, refreshToken, user, login, logout, refreshSession, hydrate.
- **App Store** (`shared/store/useAppStore.ts`): Zustand para estado da sidebar.
- **Server State**: TanStack React Query para cache e sincronização com a API.

### API Client (`shared/services/apiClient.ts`)
- Instância Axios com baseURL de `runtimeConfig.ts`
- Interceptor de request: injeta `Authorization: Bearer <token>`
- Interceptor de response: tenta refresh automático em 401
- `registerAuthSessionManager()` — Conecta o auth store ao apiClient

### Runtime Config (`shared/config/runtimeConfig.ts`)
- `API_URL` — URL da API (via `window.RUNTIME_ENV.API_URL`, `VITE_API_URL`, ou fallback `http://localhost:5156`)
- Configurável em runtime via Docker (script `40-runtime-env.sh`)

---

## 5. Banco de Dados

- **PostgreSQL 15** (Alpine) via Docker
- **Connection string dev**: `Host=localhost;Port=5432;Database=gestorfinanceiro_dev;Username=postgres;Password=postgres`
- **Migrations**: EF Core, aplicadas automaticamente no startup via `MigrateDatabaseStartupTask`
- **Seed automático**: Admin user + categorias de sistema

### Tabelas Principais
| Tabela                | Entidade Mapeada       |
|-----------------------|----------------------|
| `Accounts`            | Account              |
| `CreditCardDetails`   | CreditCardDetails (owned by Account) |
| `Transactions`        | Transaction          |
| `Categories`          | Category             |
| `Users`               | User                 |
| `RefreshTokens`       | RefreshToken         |
| `RecurrenceTemplates` | RecurrenceTemplate   |
| `AuditLogs`           | AuditLog             |
| `OperationLogs`       | OperationLog         |

---

## 6. Testes

### Backend
| Projeto                                  | Tipo                | Framework              | Descrição                                        |
|------------------------------------------|---------------------|------------------------|--------------------------------------------------|
| `GestorFinanceiro.Financeiro.UnitTests`           | Unitário            | xUnit + NSubstitute    | Testa entidades, domain services, command handlers, validators |
| `GestorFinanceiro.Financeiro.IntegrationTests`    | Integração (DB)     | xUnit + Testcontainers | Testa repositories + EF Core contra PostgreSQL real |
| `GestorFinanceiro.Financeiro.HttpIntegrationTests`| Integração (HTTP)   | xUnit + WebApplicationFactory | Testa controllers via HTTP com banco real |
| `GestorFinanceiro.Financeiro.End2EndTests`        | E2E                 | xUnit                  | (Estrutura criada, ainda não implementado)       |

**Rodar testes backend:**
```bash
cd backend
dotnet test                              # Todos
dotnet test --filter "UnitTests"         # Só unitários
dotnet test --filter "IntegrationTests"  # Só integração (requer Docker)
```

### Frontend
- **Framework**: Vitest + Testing Library + MSW
- **Setup**: `src/shared/test/setup.ts` (configura MSW server, cleanup, polyfills)
- **Mocks**: `src/shared/test/mocks/` (handlers e server MSW)
- **Cobertura**: V8, relatório HTML em `coverage/`

**Rodar testes frontend:**
```bash
cd frontend
npm test                  # Todos
npm run test:watch        # Watch mode
```

---

## 7. Desenvolvimento Local

### Debug Full Stack (VS Code F5)
Configurado em `.vscode/launch.json` e `.vscode/tasks.json`:
1. Sobe PostgreSQL via Docker (`scripts/debug/start-db.sh`)
2. Builda e inicia backend .NET em modo Debug (porta 5156)
3. Inicia frontend Vite em `http://localhost:5173` (com proxy para API)
4. Abre Chrome com debugger

### Scripts de Debug (`scripts/debug/`)
| Script              | Descrição                                    |
|--------------------|----------------------------------------------|
| `start-all.sh`    | Sobe banco + backend + frontend              |
| `start-db.sh`     | Sobe apenas o PostgreSQL via Docker           |
| `start-backend.sh`| Sobe apenas o backend .NET                    |
| `start-frontend.sh`| Sobe apenas o frontend Vite                  |
| `stop-all.sh`     | Para tudo                                     |
| `stop-db.sh`      | Para o container do PostgreSQL                |

### Credenciais de Dev
- **Banco**: postgres/postgres, database=gestorfinanceiro_dev
- **Admin**: admin@gestorfinanceiro.local / Admin@Dev123!
- **JWT Secret** (dev): `DEV_ONLY_32_PLUS_CHARACTERS_SECRET_KEY_2026_NOT_FOR_PRODUCTION`

### Portas
| Serviço   | Porta (dev) | Porta (Docker Compose) |
|-----------|-------------|----------------------|
| Frontend  | 5173        | 8080                 |
| Backend   | 5156        | 8081                 |
| PostgreSQL| 5432        | (interno)            |

### Proxy do Vite
Em desenvolvimento, o Vite faz proxy de `/api` para `http://localhost:5156` (configurado em `vite.config.ts`).

---

## 8. Deploy (Docker)

### Docker Compose (`docker-compose.yml`)
3 serviços: `db` (PostgreSQL), `api` (backend .NET), `web` (frontend Nginx)

**Variáveis de ambiente obrigatórias**:
- `JWT_SECRET` — Chave JWT (mínimo 32 bytes)
- `POSTGRES_PASSWORD` — Senha do PostgreSQL
- `ADMIN_PASSWORD` — Senha do admin inicial

### Runtime Config do Frontend
O frontend usa `runtime-env.js` injetado pelo Nginx via script `40-runtime-env.sh`, permitindo configurar `API_URL` em runtime sem rebuild.

---

## 9. Conceitos de Negócio Importantes

### Transações
- **Avulsa**: Transação única de débito ou crédito
- **Parcelada** (`InstallmentGroupId`): Grupo de N parcelas com `InstallmentNumber` e `TotalInstallments`
- **Recorrente** (`RecurrenceTemplateId`): Gerada automaticamente pelo `RecurrenceMaintenanceWorker` baseado no template
- **Transferência** (`TransferGroupId`): Par de transações (débito na origem, crédito no destino)
- **Ajuste** (`IsAdjustment`, `OriginalTransactionId`): Correge valor de transação já paga. Cria nova transação com diferença
- **Cancelamento**: Soft-cancel com razão, reverte saldo se já estava paga

### Cartão de Crédito
- Conta do tipo `Cartao` com `CreditCardDetails` (limite, dia fechamento, dia vencimento, conta débito)
- Faturas calculadas por período de fechamento
- Pagamento de fatura gera transferência para conta de débito vinculada
- Categoria especial "Pagamento de Fatura" (sistema, imutável)

### Categorias
- Tipos: Receita ou Despesa
- Categorias do sistema (`IsSystem=true`): Não podem ser editadas/excluídas (ex: "Pagamento de Fatura")
- Categorias do usuário: Customizáveis livremente

### Orçamentos (planejado)
- PRD em `tasks/prd-orcamentos/prd.md`
- Limites mensais por categoria com acompanhamento de gastos

### Projeção Financeira (planejado)
- PRD em `tasks/prd-projecao-financeira/prd.md`

---

## 10. Organização de Tasks

O projeto usa um sistema de tasks baseado em PRDs em `tasks/`:

```
tasks/<prd-nome>/
├── prd.md              # Product Requirements Document
├── techspec.md         # Especificação técnica
├── tasks.md            # Lista de tasks derivadas do PRD
├── 1_task.md           # Task 1 com critérios de aceite
├── 1_task_review.md    # Review da task 1
├── 2_task.md           # Task 2...
└── ...
```

### PRDs Existentes
| PRD                    | Status      | Descrição                                       |
|------------------------|-------------|------------------------------------------------|
| `prd-core-financeiro`  | Concluído   | Entidades base, transações, contas, categorias  |
| `prd-frontend-basico`  | Concluído   | Interface React base                            |
| `prd-api-completa`     | Concluído   | Endpoints completos da API                      |
| `prd-cartao-credito`   | Concluído   | Cartão de crédito, faturas, parcelas            |
| `prd-polimento`        | Concluído   | Refinamentos de UX e qualidade                  |
| `prd-orcamentos`       | Planejado   | Orçamentos mensais por categoria                |
| `prd-projecao-financeira`| Planejado | Projeção e previsão financeira                  |

### Templates (`templates/`)
- `prd-template.md` — Template de PRD
- `techspec-template.md` — Template de especificação técnica
- `tasks-template.md` — Template de lista de tasks
- `task-template.md` — Template de task individual
- `bug-report-template.md` — Template de relatório de bug

---

## 11. Regras e Padrões (`rules/`)

Regras de codificação e arquitetura documentadas:
- `dotnet-architecture.md` — Padrão de camadas, DI, CQRS
- `dotnet-coding-standards.md` — Convenções C#
- `dotnet-testing.md` — Padrões de teste backend
- `react-project-structure.md` — Estrutura de features e shared
- `react-coding-standards.md` — Convenções React/TypeScript
- `react-testing.md` — Padrões de teste frontend
- `restful.md` — Padrões REST da API
- `git-commit.md` — Convenção de commits
- `ux-labels-financeiros.md` — Nomenclatura de labels financeiros (pt-BR)

---

## 12. Referência Rápida para Agentes

### Onde encontrar o que preciso?

| Preciso de...                        | Caminho                                                                   |
|--------------------------------------|--------------------------------------------------------------------------|
| Entidades de domínio                 | `backend/3-Domain/.../Entity/`                                           |
| Regras de negócio                    | `backend/3-Domain/.../Service/` e `Entity/` (métodos da entidade)        |
| Enums (tipos, status)               | `backend/3-Domain/.../Enum/`                                             |
| Domain exceptions                    | `backend/3-Domain/.../Exception/`                                        |
| Interfaces de repositório            | `backend/3-Domain/.../Interface/`                                        |
| Command/Query handlers              | `backend/2-Application/.../Commands/` e `Queries/`                       |
| Validators                           | Junto do command em `backend/2-Application/.../Commands/<Feature>/`      |
| Response DTOs (backend)             | `backend/2-Application/.../Dtos/`                                        |
| Request DTOs (API input)            | `backend/1-Services/.../Controllers/Requests/`                           |
| Controllers (endpoints)             | `backend/1-Services/.../Controllers/`                                    |
| Configuração do DI                  | `backend/4-Infra/.../DependencyInjection/ServiceCollectionExtensions.cs` |
| EF Core DbContext                   | `backend/4-Infra/.../Context/FinanceiroDbContext.cs`                     |
| EF Core Config (Fluent API)         | `backend/4-Infra/.../Config/`                                            |
| Migrations                           | `backend/4-Infra/.../Migrations/`                                        |
| Implementação de repositórios       | `backend/4-Infra/.../Repository/`                                        |
| Startup tasks (seed, migrations)    | `backend/4-Infra/.../StartupTasks/`                                      |
| Testes unitários backend            | `backend/5-Tests/.../UnitTests/`                                         |
| Testes integração backend           | `backend/5-Tests/.../IntegrationTests/`                                  |
| Testes HTTP integration             | `backend/5-Tests/.../HttpIntegrationTests/`                              |
| Páginas do frontend                 | `frontend/src/features/<feature>/pages/`                                 |
| Componentes de feature              | `frontend/src/features/<feature>/components/`                            |
| Hooks (React Query)                | `frontend/src/features/<feature>/hooks/`                                 |
| API calls do frontend              | `frontend/src/features/<feature>/api/`                                   |
| Tipos TypeScript de feature         | `frontend/src/features/<feature>/types/`                                 |
| Schemas Zod (validação forms)       | `frontend/src/features/<feature>/schemas/`                               |
| Componentes UI base (shadcn)       | `frontend/src/shared/components/ui/`                                     |
| Layout (AppShell, Sidebar, Topbar) | `frontend/src/shared/components/layout/`                                 |
| API Client (Axios)                  | `frontend/src/shared/services/apiClient.ts`                              |
| Auth Store (Zustand)               | `frontend/src/features/auth/store/authStore.ts`                          |
| Rotas                               | `frontend/src/app/router/routes.tsx`                                     |
| Providers                           | `frontend/src/app/providers/`                                            |
| Config runtime (API_URL)            | `frontend/src/shared/config/runtimeConfig.ts`                            |
| Testes frontend (setup/mocks)       | `frontend/src/shared/test/`                                              |
| Docker Compose (produção)           | `docker-compose.yml`                                                     |
| Docker Compose (debug)              | `docker-compose.debug.yml`                                               |
| Regras de codificação               | `rules/`                                                                 |
| Templates de task/PRD               | `templates/`                                                             |
| PRDs e tasks de implementação       | `tasks/<prd-nome>/`                                                      |

### Padrões a seguir ao implementar

**Novo endpoint backend:**
1. Criar Request DTO em `Controllers/Requests/`
2. Criar Command/Query em `Application/Commands/` ou `Application/Queries/`
3. Criar Handler em `Application/Commands/` ou `Application/Queries/`
4. Criar Validator (se command) junto do handler
5. Criar Response DTO em `Application/Dtos/` (se novo)
6. Adicionar endpoint no Controller correspondente
7. Registrar handler no DI (via `ApplicationServiceExtensions`)
8. Adicionar testes unitários e de integração

**Nova feature frontend:**
1. Criar pasta em `features/<nome>/` com subpastas (api, components, hooks, pages, schemas, types)
2. Definir tipos TypeScript em `types/`
3. Criar funções de API em `api/`
4. Criar hooks React Query em `hooks/`
5. Criar schemas Zod em `schemas/`
6. Criar componentes em `components/`
7. Criar página em `pages/`
8. Adicionar rota em `app/router/routes.tsx`
9. Exportar via `index.ts`
10. Adicionar testes

### Subagents Disponíveis

| Agent         | Quando usar                                                    | Argumentos                          |
|---------------|---------------------------------------------------------------|-------------------------------------|
| `Implementer` | Implementar ou corrigir uma tarefa                            | `--prd`, `--techspec`, `--task`     |
| `tester`      | Testar a implementação de uma tarefa                          | `--prd`, `--techspec`, `--task`     |
| `review`      | Revisar e validar a conclusão de uma tarefa                   | `--prd`, `--techspec`, `--task`     |
| `finalizer`   | Finalizar a execução de uma tarefa                            | `--prd-dir`, `--task`               |
| `Orchestrator` | Orquestrar tarefas complexas                                 | (conforme necessidade)              |