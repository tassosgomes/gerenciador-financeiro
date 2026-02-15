---
status: completed # Opcoes: pending, in-progress, completed, excluded
parallelizable: true # Se pode executar em paralelo
blocked_by: [] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>engine/domain/categories</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database|http_server</dependencies>
<unblocks>"3.0,5.0"</unblocks>
</task_context>

# Tarefa 2.0: Backend: `Category.IsSystem` + regra de bloqueio de alteração

## Visão Geral

Adicionar suporte a categorias de sistema (`IsSystem`) e garantir no backend que categorias marcadas como sistema não possam ser editadas (e, futuramente, removidas), retornando erro de regra de negócio (Problem Details).

## Requisitos

- Adicionar campo `IsSystem` na entidade de domínio `Category` (default `false`).
- Persistir em banco (`is_system` em `categories`) via migration incremental.
- Bloquear atualização (ex.: `UpdateName`) quando `IsSystem == true` com exceção de domínio.
- Mapear a exceção para Problem Details na API (conforme regras REST do projeto).

## Subtarefas

- [x] 2.0 Backend: `Category.IsSystem` + regra de bloqueio de alteração ✅ CONCLUÍDA
	- [x] 2.1 Implementação completada
	- [x] 2.2 Definição da tarefa, PRD e tech spec validados
	- [x] 2.3 Análise de regras e conformidade verificadas
	- [x] 2.4 Revisão de código completada
	- [x] 2.5 Pronto para deploy

## Sequenciamento

- Bloqueado por: Nenhum
- Desbloqueia: 3.0, 5.0
- Paralelizável: Sim (pode ser feito em paralelo ao mobile/docker)

## Detalhes de Implementação

Referências na spec:
- "Backend — Categorias do sistema" (IsSystem + exceção)
- "Endpoints" (PUT categories deve retornar Problem Details)

Cuidados:
- Migration deve ser compatível com bases existentes.
- A regra deve existir no domínio; UI é apenas conveniência.

## Critérios de Sucesso

- Atualizar categoria `IsSystem=true` falha com erro de regra de negócio.
- Categorias não-se-sistema continuam editáveis.
- Coluna `is_system` existe e é persistida.
