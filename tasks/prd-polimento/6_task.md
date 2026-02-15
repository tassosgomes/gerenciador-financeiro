---
status: completed # Opcoes: pending, in-progress, completed, excluded
parallelizable: true # Se pode executar em paralelo
blocked_by: [] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>frontend/layout</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"7.0"</unblocks>
</task_context>

# Tarefa 6.0: Frontend: menu hambúrguer no mobile (Topbar + testes)

## Visão Geral

Implementar navegação mobile com menu hambúrguer na Topbar em telas pequenas, exibindo os mesmos itens da Sidebar (que hoje aparece apenas em `md+`). O menu deve abrir/fechar via Dialog/Drawer seguindo o design system já existente.

## Requisitos

- Em telas pequenas, mostrar botão (hambúrguer) na Topbar.
- Ao abrir, exibir os itens de navegação atuais.
- Fechar ao clicar em item/overlay e manter acessibilidade básica (focus/aria do componente usado).
- Não introduzir estado global desnecessário.

## Subtarefas

- [ ] 6.1 Implementar estado local do menu mobile na Topbar (`isOpen`, `open`, `close`)
- [ ] 6.2 Renderizar menu (Dialog/Sheet existente no projeto) com lista de rotas
- [ ] 6.3 Garantir que navegação fecha menu ao selecionar um item
- [ ] 6.4 Testes: abrir/fechar e presença dos itens (Vitest + Testing Library)

## Sequenciamento

- Bloqueado por: Nenhum
- Desbloqueia: 7.0
- Paralelizável: Sim (não depende do backend/docker)

## Detalhes de Implementação

Referência na spec:
- "Frontend — Estado do menu mobile" (estado local no Topbar)

## Critérios de Sucesso

- Em largura ~320px, menu hambúrguer permite navegar por todas as telas.
- Testes de componente passam.

## Checklist de Conclusão

- [x] 6.0 Frontend: menu hambúrguer no mobile (Topbar + testes) ✅ CONCLUÍDA
	- [x] 6.1 Implementação completada
	- [x] 6.2 Definição da tarefa, PRD e tech spec validados
	- [x] 6.3 Análise de regras e conformidade verificadas
	- [x] 6.4 Revisão de código completada
	- [x] 6.5 Pronto para deploy
