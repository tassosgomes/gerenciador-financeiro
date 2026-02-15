---
status: pending # Opcoes: pending, in-progress, completed, excluded
parallelizable: false # Se pode executar em paralelo
blocked_by: ["1.0","3.0","8.0","9.0"] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>infra/containers/compose</domain>
<type>integration</type>
<scope>configuration</scope>
<complexity>high</complexity>
<dependencies>database|http_server</dependencies>
<unblocks>"11.0"</unblocks>
</task_context>

# Tarefa 10.0: Docker: `docker-compose.yml` raiz + `.env.example` + health checks

## Visão Geral

Entregar o `docker-compose.yml` definitivo na raiz com 3 serviços (PostgreSQL + API + Web), volume persistente e health checks para todos os serviços. Incluir `.env.example` documentando variáveis obrigatórias e defaults.

## Requisitos

- `docker-compose.yml` na raiz com:
  - `db` (PostgreSQL 15+), volume persistente, sem porta exposta por padrão
  - `api` (build `./backend`), migrations automáticas no startup, health check `/health`
  - `web` (build `./frontend`), Nginx serve SPA, proxy `/api`, porta host default 8080
- Variáveis de ambiente configuráveis (porta, DB, admin seed, JWT secret).
- Health checks para db/api/web (usando `pg_isready` e requests HTTP).
- `.env.example` na raiz com tabela mínima de variáveis.

## Subtarefas

- [ ] 10.1 Criar `docker-compose.yml` raiz seguindo a spec (depends_on + condition: service_healthy)
- [ ] 10.2 Criar `.env.example` (WEB_PORT, POSTGRES_*, JWT_SECRET, ADMIN_*)
- [ ] 10.3 Validar subida end-to-end: `docker compose up -d` e health checks ficam `healthy`
- [ ] 10.4 Documentar comandos mínimos de uso (serão referenciados no README)

## Sequenciamento

- Bloqueado por: 1.0, 3.0, 8.0, 9.0
- Desbloqueia: 11.0
- Paralelizável: Não (integra e depende dos artefatos anteriores)

## Detalhes de Implementação

Referências:
- PRD F3 (requisitos 15–22)
- Spec: "docker-compose.yml (raiz)" e "Variáveis de Configuração (env)"

Cuidados:
- Não expor porta do DB por padrão.
- `JWT_SECRET` deve ser obrigatório (documentar mínimo 32 bytes).

## Critérios de Sucesso

- Em repo limpo, `docker compose up -d` sobe sem passos adicionais.
- Web responde em `http://localhost:8080` (ou porta configurada) e API funciona via proxy `/api`.
- Todos os serviços ficam `healthy`.
