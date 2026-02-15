# Especificação Técnica — Polimento e Release v1.0 (Fase 5)

## Resumo Executivo

Esta fase consolida o GestorFinanceiro como produto self-hosted “suba e use” com Docker Compose: banco PostgreSQL + API .NET + Web React servida por Nginx. A abordagem técnica foca em quatro eixos: (1) inicialização production-ready com migrations automáticas e seed idempotente (admin + categorias padrão); (2) responsividade mobile no frontend com navegação por menu hambúrguer e ajustes de layout/touch targets sem alterar o design system; (3) empacotamento final em containers com Nginx fazendo proxy reverso para a API e runtime config via `window.RUNTIME_ENV` (sem rebuild por ambiente); (4) documentação mínima de instalação/primeiro acesso e artefatos de release (CHANGELOG, tags e versionamento 1.0.0).

Decisões principais já alinhadas com o PRD e com o que existe no repo: porta padrão do web no host `8080`; UX de “trocar senha” apenas recomendada (não bloqueante); categorias seed por migration incremental (mantendo IDs existentes) e introdução de flag `IsSystem` para impedir edição/remoção das categorias do sistema.

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌─────────────────────────────────────────────────────────────────────┐
│ docker-compose.yml (raiz)                                            │
│                                                                     │
│  ┌──────────────┐     ┌─────────────────────┐     ┌──────────────┐ │
│  │ PostgreSQL 15 │<--->│ API .NET 8 (Kestrel)│<--->│ Nginx + React │ │
│  │ volume: data  │     │ /health + /api/v1  │     │ serve SPA     │ │
│  └──────────────┘     └─────────────────────┘     │ proxy /api    │ │
│                                                    └──────────────┘ │
└─────────────────────────────────────────────────────────────────────┘

Rede interna: web -> api via proxy reverso (/api -> http://api:PORT)
Usuário final: navegador -> http://localhost:8080
```

**Backend (.NET / Clean Architecture)**
- Mantém camadas: `Domain` (regras/entidades), `Application` (CQRS handlers), `Infra` (EF Core/PostgreSQL), `API` (controllers/middleware).
- Inicialização do container executa: `Database.MigrateAsync()` (migrations) → seed idempotente (admin, ajustes de categorias do sistema).

**Frontend (React + Vite + Tailwind)**
- Mantém estrutura feature-based (`frontend/src/features/*`) e layout em `shared/components/layout`.
- Nginx serve o bundle e proxya `/api/*` para o backend.
- URL da API é configurada em runtime via `API_URL` (gerando `runtime-env.js`), conforme `rules/react-containers.md`.

---

## Design de Implementação

### Interfaces Principais

#### Backend — Seed & Migrations

Para garantir migrations automáticas e seed idempotente em ambiente Docker (onde o banco pode não estar disponível imediatamente), introduzir um serviço de inicialização (Hosted Service) na API, mantendo baixo acoplamento:

```csharp
public interface IStartupTask
{
    Task RunAsync(CancellationToken ct);
}

public sealed class MigrateDatabaseStartupTask : IStartupTask
{
    private readonly FinanceiroDbContext _dbContext;

    public Task RunAsync(CancellationToken ct)
        => _dbContext.Database.MigrateAsync(ct);
}

public sealed class SeedStartupTask : IStartupTask
{
    Task RunAsync(CancellationToken ct);
}
```

Implementação pode ser agregada em um `IHostedService` que executa tasks em ordem e com retry simples (ex.: tentativas por ~30–60s) quando falhar por indisponibilidade do PostgreSQL. A execução deve ocorrer **antes** de aceitar tráfego HTTP (ex.: executando no startup, antes de `app.Run()`, ou com bloqueio de readiness via health check se preferir).

#### Frontend — Estado do menu mobile

Como o layout atual tem Sidebar visível apenas em `md+` (`hidden ... md:flex`), a solução no mobile será um menu hambúrguer na Topbar que abre um menu (Dialog) com os mesmos itens de navegação.

```ts
type MobileNavState = {
  isOpen: boolean;
  open: () => void;
  close: () => void;
};
```

A implementação pode ficar local no componente `Topbar` para evitar estado global desnecessário.

### Modelos de Dados

#### Backend — Categorias do sistema

Hoje, as categorias seed existem via migration (`SeedDefaultCategories`) mas não há flag para “sistema”. Para cumprir “system: true” e bloquear edição/remoção:

- Adicionar coluna `is_system` em `categories`.
- Mapear no Domain como `IsSystem` (default `false`).
- Para categorias seed, setar `is_system = true`.

```csharp
public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public CategoryType Type { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsSystem { get; private set; } = false;

    public void UpdateName(string newName, string userId)
    {
        if (IsSystem)
        {
            throw new SystemCategoryCannotBeChangedException(Id);
        }

        Name = newName;
        SetAuditOnUpdate(userId);
    }
}
```

Observação: hoje não existe endpoint de DELETE para categorias; mesmo assim, o bloqueio deve existir no domínio/handler para prevenir futuras rotas.

Além do backend, o frontend deve receber em `CategoryResponse` um campo booleano (ex.: `isSystem`) para desabilitar ações de edição quando aplicável. Isso melhora a UX, mas a regra deve permanecer garantida no backend.

#### Backend — Admin seed

O seed do admin já existe na API (ver `AdminSeed:*` em config). Ajustes para v1.0:
- Defaults de credenciais conforme PRD (`admin@GestorFinanceiro.local` / `mudar123`) e com aviso em log.
- Execução idempotente: só criar admin se **não houver usuários**.
- Garantir `MustChangePassword = true` (já é default no Domain).

### Endpoints de API

Sem novos endpoints obrigatórios para Docker/seed.

Ajustes esperados:
- `PUT /api/v1/categories/{id}` deve retornar erro de regra de negócio (Problem Details) ao tentar atualizar categoria `IsSystem == true`.
- `GET /health` já existe e será usado para health check no Compose.

### Empacotamento (Docker)

#### docker-compose.yml (raiz)

Adicionar `docker-compose.yml` na raiz com os 3 serviços e volume persistente do PostgreSQL.

Pontos obrigatórios:
- **PostgreSQL**: sem porta exposta por padrão; volume `postgres_data`.
- **API**: build multi-stage (Dockerfile novo no backend), expor apenas na rede interna; executar migrations e seed no startup.
- **Web**: Nginx servindo SPA; proxy reverso `/api` para a API; porta do host configurável (default 8080).

Exemplo (esqueleto, nomes podem variar):

```yaml
services:
  db:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-gestorfinanceiro}
      POSTGRES_USER: ${POSTGRES_USER:-postgres}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgres}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U $$POSTGRES_USER -d $$POSTGRES_DB"]
      interval: 5s
      timeout: 3s
      retries: 20

  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      JwtSettings__SecretKey: ${JWT_SECRET}
      AdminSeed__Name: ${ADMIN_NAME:-Administrador}
      AdminSeed__Email: ${ADMIN_EMAIL:-admin@GestorFinanceiro.local}
      AdminSeed__Password: ${ADMIN_PASSWORD:-mudar123}
    depends_on:
      db:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://localhost:8080/health > /dev/null || exit 1"]
      interval: 10s
      timeout: 3s
      retries: 20

  web:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    environment:
      API_URL: /api
    ports:
      - "${WEB_PORT:-8080}:80"
    depends_on:
      api:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://localhost/ > /dev/null || exit 1"]
      interval: 10s
      timeout: 3s
      retries: 20

volumes:
  postgres_data:
```

#### Nginx (proxy reverso + SPA)

Adicionar um arquivo de config versionado (ex.: `frontend/docker/nginx.conf`) e copiar no Dockerfile para `/etc/nginx/conf.d/default.conf`.

Regras mínimas:
- `location /api/ { proxy_pass http://api:8080/; }`
- `try_files $uri $uri/ /index.html;` para suportar React Router.

---

## Pontos de Integração

- **PostgreSQL (docker-compose)**: a API usa `ConnectionStrings:DefaultConnection` (Npgsql). Em compose, construir esta connection string a partir de env vars e injetar no container.
- **Nginx (frontend)**: precisa de config explícita para:
  - Proxy `/api/` para `http://api:<porta-interna>`.
  - SPA fallback (React Router): qualquer rota não-asset deve servir `index.html`.
- **Runtime env (frontend)**: manter o padrão atual (script `40-runtime-env.sh`) e definir `API_URL=/api` no compose para evitar CORS e manter mesma origem.

---

## Variáveis de Configuração (env)

Adicionar `.env.example` na raiz documentando pelo menos:
- `WEB_PORT` (default 8080)
- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`
- `JWT_SECRET` (obrigatório; mínimo 32 bytes)
- `ADMIN_NAME`, `ADMIN_EMAIL`, `ADMIN_PASSWORD` (defaults alinhados ao PRD)

No backend, usar a convenção de env vars do .NET com `__` para mapear seções (`JwtSettings__SecretKey`, `ConnectionStrings__DefaultConnection`, `AdminSeed__Email`, etc.).

---

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
| --- | --- | --- | --- |
| API startup (`Program.cs`) | Mudança de inicialização | Adicionar migrations automáticas e reorganizar seed. Risco médio (ordem de execução). | Implementar startup task/hosted service + testes. |
| Infra Migrations | Mudança de esquema | Adicionar `is_system` em `categories` + migration incremental para novas categorias/renome. Risco médio (bases existentes). | Migration com SQL idempotente e compatível. |
| Category Domain/Handler | Regra de negócio | Bloquear alteração de categoria de sistema. Risco baixo. | Exceção de domínio + mapeamento para Problem Details. |
| Frontend layout (`Topbar`/`Sidebar`) | UX responsivo | Menu hambúrguer no mobile e ajustes de spacing/touch. Risco baixo-médio (regressão visual). | Ajustes Tailwind + testes de UI. |
| Docker assets | Infra | Adicionar `docker-compose.yml`, `.env.example`, Dockerfile da API, Nginx conf. Risco médio (orquestração). | Validar `docker compose up -d` do zero. |
| Docs (README/CHANGELOG/LICENSE) | Documentação | Atualizações para self-host e release. Risco baixo. | Atualizar arquivos e validar instruções. |

---

## Abordagem de Testes

### Testes Unitários

Backend:
- `Category.UpdateName` deve falhar quando `IsSystem == true`.
- Handler `UpdateCategoryCommandHandler` deve mapear exceção e não persistir.

Frontend:
- Testar comportamento do menu mobile (abre/fecha, itens presentes) usando Vitest + Testing Library, seguindo padrões existentes (`Sidebar.test.tsx`, `AppShell.test.tsx`).

### Testes de Integração

Backend:
- Expandir/ajustar `CategorySeedTests` para refletir o novo conjunto de categorias (10 despesa + 4 receita) e validar `is_system = true`.
- Teste de inicialização (opcional) garantindo que `Database.MigrateAsync()` ocorre em boot quando apontado para DB vazio.

Docker smoke test (manual/CI futuro):
- `docker compose up -d` e validar health checks (web e api) com timeout.

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

1. **Backend migrations automáticas**: adicionar startup task/hosted service com `MigrateAsync` + retry e logging.
2. **Categorias do sistema**: adicionar `IsSystem` no Domain/Infra; migration para coluna + backfill; bloquear update.
3. **Seed incremental de categorias**: migration nova para inserir “Serviços” e “Impostos” e atualizar “Investimento” → “Investimentos” (mantendo IDs existentes), usando SQL idempotente.
4. **Admin seed**: ajustar defaults e logs; garantir idempotência e recomendação de troca de senha.
5. **Frontend mobile**: implementar menu hambúrguer e ajustes de layout; garantir tabelas com overflow e botões com tamanho adequado no mobile.
6. **Docker Compose + Nginx**: adicionar Dockerfile da API + nginx config + compose + `.env.example`.
7. **Documentação**: README Quick Start, tabela de variáveis de ambiente e instruções de backup/restore.
8. **Release v1.0.0**: `CHANGELOG.md`, `LICENSE` (MIT) e padronização de tags/nomes de imagem (ex.: `gestorfinanceiro-api:1.0.0`, `gestorfinanceiro-web:1.0.0`).

### Dependências Técnicas

- Docker Compose v2+
- PostgreSQL 15+ (imagem oficial)
- .NET 8 SDK/runtime para build da API
- Node 20 para build do frontend (já usado)

---

## Monitoramento e Observabilidade

- **Health checks**:
  - API: usar `/health` já mapeado.
  - DB: health check no container PostgreSQL via `pg_isready`.
  - Web: health check simples via GET `/` (Nginx).
- **Logs**:
  - Manter JSON console logging do backend (já configurado).
  - Garantir logs explícitos de migrations/seed (início/fim, tentativas, resultado) sem vazar segredos.

---

## Considerações Técnicas

### Decisões Principais

- **Porta padrão web no host: 8080** (decisão de produto para onboarding).
- **Troca de senha no primeiro login: recomendação (não bloqueante)**.
  - Backend mantém `MustChangePassword`.
  - Frontend exibe banner/modal recomendando troca sem travar o usuário.
- **Seed de categorias via migration incremental** (manter IDs existentes) para compatibilidade com installs já feitos.
- **Nginx como único ponto de entrada**: browser acessa web; web proxya `/api` para a API; `API_URL=/api` no runtime-env.

### Riscos Conhecidos

- **Migrations no startup + dependência do Postgres**: risco de race condition na subida do compose.
  - Mitigação: retry/backoff e health checks; `depends_on` com `condition: service_healthy`.
- **Renomear “Investimento” → “Investimentos”** em bases existentes pode impactar relatórios/expectativas do usuário.
  - Mitigação: migration que atualiza apenas a categoria seed por ID fixo; documentar no changelog.
- **Responsividade**: regressões em telas desktop.
  - Mitigação: mudanças com breakpoints claros (`sm/md/lg`) e testes de componentes críticos.

### Requisitos Especiais

- **Segurança**:
  - `JwtSettings:SecretKey` obrigatório e com mínimo 32 bytes (já validado no startup).
  - Admin seed e senha padrão devem ser configuráveis por env vars; documentar fortemente a troca.
- **Containers**:
  - Multi-stage builds para imagens menores (já no frontend; implementar no backend).
  - Evitar expor portas do DB.

- **Release v1.0.0 (processo)**:
  - Gerar `CHANGELOG.md` cobrindo as fases do MVP.
  - Criar tag `v1.0.0` no repositório.
  - Buildar imagens com tags de versão (ex.: `gestorfinanceiro-api:1.0.0`, `gestorfinanceiro-web:1.0.0`).

### Conformidade com Padrões

- **.NET / Clean Architecture e CQRS**: manter camadas e dispatcher nativo, conforme `rules/dotnet-architecture.md`.
- **Erros HTTP**: regras de negócio (categoria system) devem retornar Problem Details (RFC 9457), conforme `rules/restful.md` e middleware existente.
- **React**: manter estrutura feature-based e layout em `shared/`, conforme `rules/react-project-structure.md`.
- **Containers (frontend)**: runtime config via `window.RUNTIME_ENV` e script de geração em startup do Nginx, conforme `rules/react-containers.md`.
- **Boas práticas de container**: multi-stage e tags fixas de imagem quando possível, conforme `rules/container-bestpratices.md`.
