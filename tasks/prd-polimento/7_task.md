---
status: pending # Opcoes: pending, in-progress, completed, excluded
parallelizable: true # Se pode executar em paralelo
blocked_by: ["6.0"] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>frontend/responsive</domain>
<type>implementation</type>
<scope>performance</scope>
<complexity>high</complexity>
<dependencies>http_server</dependencies>
<unblocks>"11.0"</unblocks>
</task_context>

# Tarefa 7.0: Frontend: responsividade 320px+ (tabelas/forms/dashboard/touch)

## Visão Geral

Garantir que as telas principais do frontend sejam utilizáveis em 320px+, com foco em:
- tabelas com scroll horizontal ou layout alternativo
- formulários empilhados verticalmente no mobile
- dashboard com cards em coluna única
- botões com área de toque mínima de 44x44px

## Requisitos

- Todas as telas devem funcionar em 320px de largura.
- Tabelas não podem "estourar" layout; devem permitir scroll horizontal.
- Formulários devem empilhar campos no mobile.
- Dashboard reorganiza cards em coluna única no mobile.
- Touch targets mínimos (44x44px) para botões/ações frequentes.
- Manter design system (não criar novos tokens/cores).

## Subtarefas

- [ ] 7.1 Mapear telas críticas (dashboard, transações, categorias, contas, etc.) e pontos de quebra
- [ ] 7.2 Ajustar tabelas (overflow-x, wrappers) e garantir legibilidade
- [ ] 7.3 Ajustar formulários (grid → stack em `sm`/`md`), inputs/botões e espaçamentos
- [ ] 7.4 Ajustar dashboard (cards em coluna única) e gráficos responsivos
- [ ] 7.5 Testes: atualizar/adicionar testes de componentes críticos quando aplicável

## Sequenciamento

- Bloqueado por: 6.0
- Desbloqueia: 11.0
- Paralelizável: Sim (depende apenas do layout mobile já existir)

## Detalhes de Implementação

Referências:
- PRD F2 (requisitos 7–14)
- Spec: "Frontend mobile" (breakpoints, touch targets, tabelas)

## Critérios de Sucesso

- Teste manual: app usável em viewport 320px sem sobreposições/overflow quebrando.
- Ações principais têm botões clicáveis confortáveis.
- Mudanças não degradam experiência em desktop.
