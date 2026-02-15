---
status: completed # Opcoes: pending, in-progress, completed, excluded
parallelizable: false # Se pode executar em paralelo
blocked_by: ["2.0"] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>engine/infra/migrations</domain>
<type>integration</type>
<scope>configuration</scope>
<complexity>high</complexity>
<dependencies>database</dependencies>
<unblocks>"5.0,10.0"</unblocks>
</task_context>

# Tarefa 3.0: Backend: migration incremental das categorias padrão v1.0

## Visão Geral

Criar uma migration incremental (compatível com bases existentes) para alinhar as categorias padrão ao PRD v1.0:
- Despesa (10): Alimentação, Transporte, Moradia, Saúde, Educação, Lazer, Vestuário, Serviços, Impostos, Outros
- Receita (4): Salário, Freelance, Investimentos, Outros
E marcar as categorias seed como `IsSystem=true`.

## Requisitos

- Inserir categorias novas (ex.: Serviços, Impostos) sem duplicar em reexecuções.
- Renomear "Investimento" → "Investimentos" mantendo o ID existente (quando aplicável).
- Garantir `is_system=true` para categorias seed.
- SQL idempotente/defensivo (não quebrar se já existir).

## Subtarefas

- [x] 3.1 Levantar IDs/nomes atuais das categorias seed existentes (para manter compat)
- [x] 3.2 Criar migration com SQL idempotente (INSERT ... WHERE NOT EXISTS, UPDATE por ID fixo)
- [x] 3.3 Atualizar/expandir testes de seed/migration (ex.: `CategorySeedTests`) para novo conjunto
- [x] 3.4 Validar em base "antiga" (com dados) e base "vazia" (fresh) que resultado converge

## Sequenciamento

- Bloqueado por: 2.0
- Desbloqueia: 5.0, 10.0
- Paralelizável: Não (depende do modelo `IsSystem` e impacta integração/compose)

## Detalhes de Implementação

Referências na spec:
- "Seed incremental de categorias" (migration incremental; manter IDs)
- "Riscos" (renome impacta expectativas; mitigar atualizando por ID seed)

## Critérios de Sucesso

- Após aplicar migrations em DB vazio, existem exatamente 14 categorias padrão com `is_system=true`.
- Em DB existente, migration não duplica e renomeia apenas a categoria seed-alvo.
- Testes de seed/migration passam e cobrem o novo conjunto.
