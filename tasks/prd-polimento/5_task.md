---
status: pending # Opcoes: pending, in-progress, completed, excluded
parallelizable: true # Se pode executar em paralelo
blocked_by: ["2.0"] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>frontend/features/categories</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>low</complexity>
<dependencies>http_server</dependencies>
<unblocks>"7.0,11.0"</unblocks>
</task_context>

# Tarefa 5.0: Frontend: suportar `isSystem` e desabilitar edição de categoria

## Visão Geral

Atualizar o frontend para consumir o campo `isSystem` na resposta de categorias e desabilitar ações de edição quando `isSystem == true`, melhorando a UX e refletindo a regra do backend.

## Requisitos

- DTO/model de categoria deve incluir booleano `isSystem` (nome conforme contrato existente).
- Ações de UI de edição/rename devem estar desabilitadas/ocultas para categorias do sistema.
- Não alterar o design system (usar componentes/tokens existentes).

## Subtarefas

- [ ] 5.1 Atualizar client/API typings para incluir `isSystem`
- [ ] 5.2 Ajustar componentes de lista/edição para bloquear ações quando `isSystem=true`
- [ ] 5.3 Ajustar tratamento de erro: se backend retornar Problem Details ao editar sistema, exibir mensagem adequada
- [ ] 5.4 Testes: unit/component (Vitest + Testing Library) cobrindo o bloqueio

## Sequenciamento

- Bloqueado por: 2.0
- Desbloqueia: 7.0, 11.0
- Paralelizável: Sim (independente do menu mobile; depende só do contrato backend)

## Detalhes de Implementação

Referência na spec:
- "Frontend — Categorias do sistema" (expor campo e desabilitar ações)

## Critérios de Sucesso

- Categorias com `isSystem=true` não exibem/permitem edição na UI.
- Tentativas de edição (se ocorrerem) mostram erro amigável.
- Testes passam.
