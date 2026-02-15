---
status: completed # Opcoes: pending, in-progress, completed, excluded
parallelizable: true # Se pode executar em paralelo
blocked_by: [] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>infra/containers/frontend</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 9.0: Docker: Nginx config (proxy `/api` + SPA fallback) + runtime env

## Visão Geral

Versionar uma configuração Nginx para servir o bundle React e fazer proxy reverso de `/api/*` para a API, além de garantir fallback SPA (React Router). Manter runtime config via `window.RUNTIME_ENV` conforme padrão do repo.

## Requisitos

- Nginx deve:
  - proxy `/api/` → `http://api:<porta>/`
  - servir SPA com `try_files ... /index.html`
- Manter runtime env do frontend (ex.: `API_URL=/api`) sem rebuild por ambiente.

## Subtarefas

- [x] 9.0 Docker: Nginx config (proxy `/api` + SPA fallback) + runtime env ✅ CONCLUÍDA
  - [x] 9.1 Implementação completada
  - [x] 9.2 Definição da tarefa, PRD e tech spec validados
  - [x] 9.3 Análise de regras e conformidade verificadas
  - [x] 9.4 Revisão de código completada
  - [x] 9.5 Pronto para deploy

## Sequenciamento

- Bloqueado por: Nenhum
- Desbloqueia: 10.0
- Paralelizável: Sim

## Detalhes de Implementação

Referências:
- Spec: "Nginx (proxy reverso + SPA)" e "Runtime env (frontend)"

## Critérios de Sucesso

- `GET /` retorna SPA.
- `GET /api/health` (via web) alcança a API sem CORS.
- Refresh em rota do React Router funciona (sem 404 do Nginx).
