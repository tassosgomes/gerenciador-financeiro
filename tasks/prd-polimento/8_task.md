---
status: pending # Opcoes: pending, in-progress, completed, excluded
parallelizable: true # Se pode executar em paralelo
blocked_by: [] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>infra/containers/backend</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 8.0: Docker: Dockerfile da API (.NET) multi-stage

## Visão Geral

Criar/ajustar um Dockerfile no backend para build multi-stage da API .NET 8, visando imagem menor e pronta para execução no docker-compose.

## Requisitos

- Multi-stage build (restore/build/publish → runtime).
- Expor a porta interna usada no compose (ex.: 8080 no container) via `ASPNETCORE_URLS`.
- Não incluir ferramentas de dev no runtime.

## Subtarefas

- [ ] 8.1 Criar Dockerfile no `backend/` com stages de build e runtime
- [ ] 8.2 Garantir que o entrypoint roda a API normalmente (migrations/seed rodarão no startup da app)
- [ ] 8.3 Validar build local de imagem (manual) e tamanho aproximado (opcional)

## Sequenciamento

- Bloqueado por: Nenhum
- Desbloqueia: 10.0
- Paralelizável: Sim (independe do frontend)

## Detalhes de Implementação

Referências:
- PRD F3 (Dockerfile otimizado, multi-stage)
- Spec: "Empacotamento (Docker)" e "Boas práticas de container"

## Critérios de Sucesso

- `docker build` do backend completa e a imagem sobe com a API.
- Não há dependências de SDK no runtime final.
