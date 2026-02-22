# GestorFinanceiro

Sistema de gestão financeira pessoal/familiar self-hosted, desenvolvido com .NET 8 (backend) e React + Vite (frontend), com banco de dados PostgreSQL.

## 📋 Visão Geral

O GestorFinanceiro é uma solução completa para controle de finanças pessoais e familiares, projetado para ser executado em infraestrutura própria via Docker. Permite gerenciar múltiplas contas bancárias, categorizar receitas e despesas, controlar orçamentos mensais por categoria e visualizar dashboards com análises financeiras.

**Principais funcionalidades:**

- **Gestão de Contas Bancárias**: Cadastro e acompanhamento de saldos de múltiplas contas
- **Transações**: Registro de receitas e despesas com categorização
- **Categorias**: Sistema de categorias personalizáveis com suporte a categorias padrão do sistema
- **Orçamentos**: Definição e acompanhamento de limites mensais por categoria
- **Dashboard**: Visualização consolidada com gráficos e métricas financeiras
- **Controle de Acesso**: Sistema de autenticação JWT com perfis de usuário (Admin/Membro)
- **Interface Responsiva**: Design adaptável para desktop, tablet e celular (mínimo 320px)

## 🎯 Para Quem é Este Sistema

Famílias ou indivíduos que desejam:
- Total controle e privacidade dos dados financeiros (self-hosted)
- Solução sem custos mensais de assinatura
- Flexibilidade para customizar categorias e relatórios
- Acesso multi-usuário com controle de permissões

## 🚀 Quick Start

### Pré-requisitos

- Docker 20.10+
- Docker Compose v2+
- 2GB RAM disponível (mínimo recomendado)
- Portas disponíveis: 8080 (padrão, configurável)

### Instalação

1. **Clone o repositório:**
   ```bash
   git clone https://github.com/seu-usuario/gestor-financeiro.git
   cd gestor-financeiro
   ```

2. **Configure as variáveis de ambiente:**
   ```bash
   cp .env.example .env
   ```

3. **Edite o arquivo `.env` e configure as variáveis obrigatórias:**
   - `JWT_SECRET`: Gere uma chave segura (mínimo 32 bytes)
     ```bash
     # Linux/Mac:
     openssl rand -base64 32
     
     # PowerShell:
     [Convert]::ToBase64String((1..32|%{Get-Random -Maximum 256}))
     ```
   - `POSTGRES_PASSWORD`: Altere para uma senha forte
   - `ADMIN_PASSWORD`: Defina a senha do administrador inicial

4. **Suba a aplicação:**
   ```bash
   docker compose up -d
   ```

5. **Aguarde os serviços iniciarem** (30-60 segundos) e acesse:
   ```
   http://localhost:8080
   ```

## 🧪 Execução Local em Debug (Frontend + Backend + Banco)

Quando quiser debugar localmente com hot reload no frontend e backend, use os scripts em `scripts/debug`.

### Pré-requisitos para debug local

- Docker (para o PostgreSQL)
- .NET SDK 8+
- Node.js 18+ e npm

### Subir tudo em debug (recomendado)

```bash
./scripts/debug/start-all.sh
```

### Debug com 1 clique no VS Code (F5)

Também foi configurado debug via VS Code em `.vscode/launch.json` e `.vscode/tasks.json`:

- Configuração: `Debug Full Stack`
- Atalho: `F5`

O fluxo automático no F5:
- sobe o banco (`db`) com porta publicada no host;
- builda e inicia o backend em modo Debug;
- inicia o frontend com Vite em `http://localhost:5173`;
- abre o frontend no Chrome com debugger anexado.

Esse comando:
- sobe apenas o serviço `db` no Docker Compose;
- aplica o override `docker-compose.debug.yml` para expor o PostgreSQL no host;
- garante que o database exista;
- inicia o backend com `dotnet watch` em `http://localhost:5156`;
- inicia o frontend com Vite em `http://localhost:5173`.

### Comandos separados

```bash
./scripts/debug/start-db.sh
./scripts/debug/start-backend.sh
./scripts/debug/start-frontend.sh
./scripts/debug/stop-db.sh
```

### Portas e variáveis opcionais

Você pode ajustar no `.env`:
- `API_PORT` (padrão `5156`)
- `FRONTEND_PORT` (padrão `5173`)
- `DB_PORT` (padrão `5432`)
- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`

### Primeiro Acesso

1. Faça login com as credenciais configuradas no `.env`:
   - **Email**: valor de `ADMIN_EMAIL` (padrão: `admin@gestorfinanceiro.local`)
   - **Senha**: valor de `ADMIN_PASSWORD` (padrão: `mudar123`)

2. **⚠️ IMPORTANTE**: Altere a senha do administrador imediatamente após o primeiro login:
   - Acesse o menu de perfil (canto superior direito)
   - Selecione "Alterar Senha"
   - Defina uma senha forte

3. O sistema já vem com **categorias padrão** pré-configuradas:
   - **Despesas**: Alimentação, Transporte, Moradia, Saúde, Educação, Lazer, Vestuário, Serviços, Impostos, Outros
   - **Receitas**: Salário, Freelance, Investimentos, Outros

4. Comece a usar:
   - Cadastre suas contas bancárias
   - Registre suas primeiras transações
   - Configure orçamentos mensais
   - Convide membros da família (se aplicável)

## ⚙️ Configuração

### Variáveis de Ambiente

| Variável | Descrição | Padrão | Obrigatório |
|----------|-----------|--------|-------------|
| `WEB_PORT` | Porta do host para acesso à aplicação web | `8080` | Não |
| `API_PORT` | Porta do host para acesso direto à API | `5156` | Não |
| `POSTGRES_DB` | Nome do banco de dados PostgreSQL | `gestorfinanceiro` | Não |
| `POSTGRES_USER` | Usuário do banco de dados | `postgres` | Não |
| `POSTGRES_PASSWORD` | Senha do banco de dados | `postgres` | ⚠️ Sim (altere!) |
| `JWT_SECRET` | Chave secreta para assinatura de tokens JWT (mínimo 32 bytes) | - | ✅ Sim |
| `ADMIN_NAME` | Nome do usuário administrador inicial | `Administrador` | Não |
| `ADMIN_EMAIL` | Email/login do administrador inicial | `admin@gestorfinanceiro.local` | Não |
| `ADMIN_PASSWORD` | Senha do administrador inicial | `mudar123` | ⚠️ Sim (altere!) |
| `OTEL_ENDPOINT` | Endpoint OpenTelemetry (opcional) | - | Não |

**Notas de Segurança:**
- `JWT_SECRET`: Deve ser uma string aleatória com no mínimo 32 bytes (256 bits). Use os comandos de geração fornecidos no Quick Start.
- `POSTGRES_PASSWORD` e `ADMIN_PASSWORD`: **Nunca** use os valores padrão em produção. Defina senhas fortes e únicas.
- Em ambientes de produção, considere usar um gerenciador de secrets (ex.: Docker Secrets, HashiCorp Vault).

### Portas e Serviços

- **Frontend (Web)**: `http://localhost:${WEB_PORT}` (padrão: 8080)
- **API**: `http://localhost:${API_PORT}` (padrão: 5156)
- **Health Check API**: `http://localhost:${API_PORT}/health`
- **PostgreSQL**: Não exposto externamente (apenas dentro da rede Docker)

## 🗄️ Backup e Restore

### Backup do Banco de Dados

Para fazer backup completo dos dados:

```bash
docker compose exec db pg_dump -U postgres gestorfinanceiro > backup-$(date +%Y%m%d-%H%M%S).sql
```

**Recomendações:**
- Execute backups regularmente (ex.: diariamente, semanalmente)
- Armazene backups fora do servidor onde o aplicativo está rodando
- Teste restaurações periodicamente para garantir integridade

### Restore do Banco de Dados

Para restaurar um backup:

```bash
# 1. Pare os serviços (para evitar transações durante restore)
docker compose down

# 2. Suba apenas o banco
docker compose up -d db

# 3. Aguarde o banco ficar pronto
docker compose exec db pg_isready -U postgres

# 4. Restore o backup
docker compose exec -T db psql -U postgres gestorfinanceiro < backup-20260215-120000.sql

# 5. Suba todos os serviços novamente
docker compose up -d
```

**⚠️ Atenção:** O restore sobrescreve todos os dados existentes. Faça backup antes se houver dados importantes.

## 🔧 Comandos Úteis

### Verificar Status dos Serviços

```bash
docker compose ps
```

### Ver Logs

```bash
# Todos os serviços
docker compose logs -f

# Serviço específico
docker compose logs -f api
docker compose logs -f web
docker compose logs -f db
```

### Reiniciar Serviços

```bash
# Todos
docker compose restart

# Específico
docker compose restart api
```

### Parar a Aplicação

```bash
docker compose down
```

### Atualizar para Nova Versão

```bash
# 1. Faça backup dos dados (ver seção Backup)

# 2. Pare os serviços
docker compose down

# 3. Atualize o código
git pull origin main

# 4. Baixe as novas imagens
docker compose pull

# 5. Rebuild (se necessário)
docker compose build

# 6. Suba novamente
docker compose up -d
```

## 🖼️ Screenshots

_(Adicione capturas de tela do sistema aqui ou mantenha links para a documentação visual)_

**Dashboard Principal:**
- Visão consolidada de saldos e transações
- Gráficos de categoria e evolução mensal

**Gestão de Transações:**
- Interface intuitiva para registro de receitas/despesas
- Filtros e busca avançada

**Orçamentos:**
- Acompanhamento visual de limites versus gastos reais

## 🏗️ Arquitetura

### Stack Tecnológico

**Backend:**
- .NET 8 (Clean Architecture + CQRS)
- Entity Framework Core 8
- PostgreSQL 15
- JWT Authentication
- Serilog (logging estruturado)

**Frontend:**
- React 18 + TypeScript
- Vite
- TanStack Query (React Query)
- Tailwind CSS
- Recharts (visualizações)

**Infraestrutura:**
- Docker & Docker Compose
- Nginx (reverse proxy + SPA serving)
- Multi-stage builds para otimização de imagens

### Estrutura do Projeto

```
.
├── backend/               # API .NET 8
│   ├── 1-Services/       # API Controllers & Startup
│   ├── 2-Application/    # CQRS Handlers & DTOs
│   ├── 3-Domain/         # Entidades e Regras de Negócio
│   ├── 4-Infra/          # EF Core & Repositórios
│   └── 5-Tests/          # Testes (Unit, Integration, E2E)
├── frontend/             # SPA React
│   ├── src/
│   │   ├── features/     # Módulos por funcionalidade
│   │   ├── shared/       # Componentes e utilitários compartilhados
│   │   └── app/          # Configuração e routing
│   └── docker/           # Configs Nginx
├── docker-compose.yml    # Orquestração dos serviços
├── .env.example          # Template de configuração
└── DOCKER.md            # Comandos Docker detalhados
```

## 🐛 Troubleshooting

### Serviços não iniciam / ficam unhealthy

1. Verifique os logs:
   ```bash
   docker compose logs
   ```

2. Confirme que as portas não estão em uso:
   ```bash
   # Linux/Mac
   lsof -i :8080
   
   # Windows PowerShell
   netstat -ano | findstr :8080
   ```

3. Verifique se há espaço em disco suficiente

### Erro de autenticação no primeiro login

- Confirme que `ADMIN_EMAIL` e `ADMIN_PASSWORD` no `.env` correspondem às credenciais que está usando
- Verifique os logs da API: `docker compose logs api`

### Migrations não executam automaticamente

- Verifique os logs de inicialização da API: `docker compose logs api | grep -i migration`
- Se necessário, execute manualmente:
  ```bash
  docker compose exec api dotnet ef database update
  ```

### Banco de dados com problemas

```bash
# Verificar se o PostgreSQL está rodando
docker compose exec db pg_isready -U postgres

# Acessar o console PostgreSQL
docker compose exec db psql -U postgres -d gestorfinanceiro
```

## 📦 Versões e Tags Docker

As imagens Docker seguem versionamento semântico:

- `gestorfinanceiro-api:1.0.0`
- `gestorfinanceiro-web:1.0.0`

Para usar uma versão específica, edite o `docker-compose.yml` e especifique a tag desejada:

```yaml
services:
  api:
    image: gestorfinanceiro-api:1.0.0
    # ou
    build:
      context: ./backend
      dockerfile: Dockerfile
```

**Tags disponíveis:**
- `latest`: Última versão estável
- `v1.0.0`: Release inicial (MVP completo)

### Processo de Tag Git (manual do maintainer)

A criação da tag de release é um passo manual e **não é executada automaticamente** por scripts do projeto.

```bash
# 1. Garanta que está na branch/revisão final da release
git checkout main
git pull --ff-only

# 2. Crie a tag anotada da versão
git tag -a v1.0.0 -m "Release v1.0.0"

# 3. Publique a tag no remoto
git push origin v1.0.0
```

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. Faça fork do repositório
2. Crie uma branch para sua feature (`git checkout -b feat/minha-feature`)
3. Commit suas mudanças seguindo o padrão de commits do projeto
4. Push para a branch (`git push origin feat/minha-feature`)
5. Abra um Pull Request

Veja o arquivo `rules/git-commit.md` para detalhes sobre o padrão de commits.

## 📄 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🆘 Suporte

Para reportar bugs, solicitar funcionalidades ou tirar dúvidas:

- Abra uma [Issue](https://github.com/seu-usuario/gestor-financeiro/issues) no GitHub
- Consulte a [documentação completa](./docs) (se disponível)
- Verifique o [CHANGELOG.md](CHANGELOG.md) para histórico de versões

---

**Desenvolvido com ❤️ para quem valoriza privacidade e controle dos próprios dados financeiros.**
