---
status: completed # Opcoes: pending, in-progress, completed, excluded
parallelizable: true # Se pode executar em paralelo
blocked_by: [] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>engine/infra/startup</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>high</complexity>
<dependencies>database|http_server|temporal</dependencies>
<unblocks>"4.0,10.0"</unblocks>
</task_context>

# Tarefa 1.0: Backend: startup tasks (migrate + seed com retry)

## Visão Geral

Implementar uma rotina de inicialização no backend (.NET 8) para aplicar migrations automaticamente e executar seed idempotente, com retry/backoff para lidar com a indisponibilidade temporária do PostgreSQL no Docker Compose.

## Requisitos

- Aplicar migrations automaticamente na inicialização (`Database.MigrateAsync`).
- Executar seed idempotente após migrations (sem duplicar dados).
- Implementar retry simples (~30–60s) quando o DB não estiver pronto.
- Não aceitar tráfego “como pronto” antes de concluir migrations/seed (preferir readiness via health check ou bloqueio de boot conforme arquitetura atual).
- Logar início/fim e tentativas sem vazar segredos.

## Subtarefas

- [x] 1.0 Backend: startup tasks (migrate + seed com retry) ✅ CONCLUÍDA
	- [x] 1.1 Implementação completada
	- [x] 1.2 Definição da tarefa, PRD e tech spec validados
	- [x] 1.3 Análise de regras e conformidade verificadas
	- [x] 1.4 Revisão de código completada
	- [x] 1.5 Pronto para deploy

## Sequenciamento

- Bloqueado por: Nenhum
- Desbloqueia: 4.0, 10.0
- Paralelizável: Sim (não depende de mudanças de domínio/UX)

## Detalhes de Implementação

Referências na spec:
- "Backend — Seed & Migrations" (IStartupTask + hosted service)
- "Riscos Conhecidos" (race condition Postgres; mitigação com retry e health checks)

Notas:
- Preferir que a inicialização rode uma única vez por boot.
- Evitar loops infinitos: limite de tempo/tentativas.
- Em caso de falha persistente, falhar o processo (para o Compose reiniciar) é aceitável.

## Critérios de Sucesso

- Subindo a API apontando para DB vazio, migrations são aplicadas automaticamente.
- Seed não duplica dados em reboots.
- Logs mostram tentativas/resultado sem expor senha/secret.
