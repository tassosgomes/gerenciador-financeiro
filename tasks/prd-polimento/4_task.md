---
status: completed # Opcoes: pending, in-progress, completed, excluded
parallelizable: true # Se pode executar em paralelo
blocked_by: ["1.0"] # IDs de tarefas que devem ser completadas primeiro
---

<task_context>
<domain>engine/infra/seed</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>database|http_server</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 4.0: Backend: seed do admin (defaults PRD + idempotência + logs)

## Visão Geral

Ajustar o seed do usuário admin para ficar alinhado ao PRD v1.0, garantindo idempotência (rodar apenas quando o banco estiver vazio/sem usuários) e logs seguros.

## Requisitos

- Credenciais padrão configuráveis via env vars, com defaults:
  - email: `admin@GestorFinanceiro.local`
  - senha: `mudar123`
- Criar admin apenas se não houver usuários (idempotente).
- Garantir `MustChangePassword = true` (se já existir comportamento, manter/validar).
- Logar aviso orientando a trocar a senha (sem imprimir senha).

## Subtarefas

- [x] 4.1 Alinhar leitura de config (`AdminSeed__*`) e defaults do PRD
- [x] 4.2 Implementar checagem idempotente (não criar se existir qualquer usuário)
- [x] 4.3 Garantir comportamento `MustChangePassword` e mensagens de log
- [x] 4.4 Testes: unit/integration garantindo idempotência e criação com defaults

## Sequenciamento

- Bloqueado por: 1.0
- Desbloqueia: 10.0
- Paralelizável: Sim (pode rodar em paralelo ao frontend e containers)

## Detalhes de Implementação

Referências na spec:
- "Backend — Admin seed" (defaults, idempotência, must-change)
- "Segurança" (não vazar secrets; JWT secret obrigatório já existente)

## Critérios de Sucesso

- Em DB vazio: admin é criado uma vez com email/nome configurados.
- Em reboots: seed não cria duplicatas.
- Logs instruem troca de senha sem vazar credenciais.
