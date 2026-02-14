# PRD — Core Financeiro (Fase 1)

## Visão Geral

O Core Financeiro é o motor do GestorFinanceiro: a camada de domínio que modela contas, categorias e transações com regras de saldo materializado, parcelamento, recorrência, transferências e auditoria. Nesta fase **não há UI nem API HTTP** — o objetivo é ter um engine financeiramente correto, coberto por testes, pronto para ser exposto nas fases seguintes.

**Problema**: Ferramentas de finanças pessoais existentes ou erram no modelo contábil (cartão de crédito tratado como conta corrente), ou não permitem self-hosted confiável. O GestorFinanceiro resolve isso com um motor contábil correto desde a fundação.

**Para quem**: Famílias que querem controle absoluto das finanças em servidor próprio, com auditoria de todas as operações.

**Valor**: Base sólida e testada que garante consistência financeira em todas as operações subsequentes.

## Objetivos

- Implementar modelo de domínio completo para contas, categorias e transações
- Garantir consistência de saldo materializado em todas as operações
- Suportar parcelamento, recorrência mensal e transferências entre contas
- Manter trilha de auditoria em todas as operações de escrita
- Atingir cobertura de testes ≥ 90% no domínio
- Não introduzir dependência de framework HTTP nesta fase

## Histórias de Usuário

- Como **membro da família**, quero cadastrar minhas contas (corrente, cartão, investimento, carteira) para organizar meu dinheiro por origem
- Como **membro da família**, quero registrar uma despesa parcelada no cartão para que todas as parcelas futuras sejam criadas automaticamente
- Como **membro da família**, quero transferir dinheiro entre contas para refletir movimentações reais (ex.: pagar fatura do cartão)
- Como **membro da família**, quero corrigir o valor de uma transação via ajuste, sem perder o registro original, para manter a auditoria
- Como **membro da família**, quero cancelar logicamente uma transação incorreta sem que ela seja apagada do histórico
- Como **membro da família**, quero criar categorias de receita e despesa para classificar minhas transações
- Como **admin da família**, quero que toda operação registre quem fez e quando, para auditoria familiar

## Funcionalidades Principais

### F1 — Contas

Modelagem de contas financeiras da família com saldo materializado.

**Requisitos funcionais:**

1. O sistema deve permitir criar uma conta com: nome, tipo, saldo inicial e flag "permitir saldo negativo"
2. Os tipos de conta suportados são: `Corrente`, `Cartão`, `Investimento`, `Carteira`
3. O saldo da conta é materializado — atualizado a cada transação confirmada (status `Paid`)
4. Uma conta pode ser ativada ou inativada; contas inativas não aceitam novas transações
5. Contas não podem ser excluídas fisicamente
6. Contas com flag "permitir saldo negativo" = false devem rejeitar transações que resultem em saldo negativo
7. Toda criação/alteração de conta deve registrar: usuário responsável e timestamp

### F2 — Categorias

Classificação de transações por tipo de receita ou despesa.

**Requisitos funcionais:**

8. O sistema deve permitir criar uma categoria com: nome e tipo (`Receita` ou `Despesa`)
9. O sistema deve permitir editar o nome de uma categoria existente
10. Categorias não podem ser excluídas fisicamente
11. Um conjunto de categorias padrão deve ser definido para seed inicial (ex.: Alimentação, Transporte, Salário, Moradia, Lazer, Saúde, Educação)
12. Toda criação/alteração de categoria deve registrar: usuário responsável e timestamp

### F3 — Transações

Engine principal de movimentações financeiras.

**Requisitos funcionais:**

13. Uma transação deve conter: conta, tipo (`Debit` / `Credit`), valor (> 0), categoria, data de competência (`CompetenceDate`), data de vencimento (`DueDate`, opcional), descrição e status
14. Os status persistidos no banco são apenas: `Paid`, `Pending` e `Cancelled`
    - `Paid` — transação efetivada (afeta saldo)
    - `Pending` — aguardando pagamento/recebimento
    - `Cancelled` — cancelada logicamente
    - `Overdue` — **não é um status persistido**; é uma condição calculada em tempo de leitura quando `DueDate` < data atual e o status persistido é `Pending`. Não requer job nem atualização periódica
15. Apenas transações com status `Paid` devem afetar o saldo materializado da conta
16. Transações não podem ser excluídas fisicamente
17. Transações não podem ser editadas diretamente após criação — alterações devem ser feitas via Ajuste (F4)
18. Transações do tipo `Debit` diminuem o saldo da conta; `Credit` aumentam
19. Toda criação de transação deve registrar: usuário responsável e timestamp

### F4 — Ajuste (Adjustment)

Mecanismo de correção imutável baseado em diferença contábil. O ajuste **nunca altera a transação original** e **nunca recalcula o histórico** — representa apenas a diferença entre o valor original e o valor correto.

**Requisitos funcionais:**

20. Para corrigir uma transação, o sistema deve criar uma nova transação compensatória do tipo `Adjustment` vinculada à original, representando apenas a **diferença** de valor
21. Exemplo: se a transação original é `Debit 100` e o valor correto é `Debit 130`, o ajuste cria apenas `Debit 30` (a diferença). Se o valor correto é `Debit 80`, o ajuste cria `Credit 20` (reversão parcial)
22. A transação original permanece inalterada e marcada como "possui ajuste"
23. O saldo da conta deve ser atualizado incrementalmente com o impacto do ajuste (`SaldoAtual = SaldoAtual + impacto`), nunca recalculando o histórico inteiro

### F5 — Cancelamento Lógico

**Requisitos funcionais:**

24. O cancelamento altera o status da transação para `Cancelled`
25. Se a transação cancelada tinha status `Paid`, o saldo da conta deve ser revertido
26. A transação cancelada permanece visível no histórico com indicação clara de cancelamento
27. O cancelamento deve registrar: usuário responsável, timestamp e motivo (opcional)

### F6 — Parcelamento (Installments)

**Requisitos funcionais:**

28. Ao criar uma transação parcelada, o sistema deve gerar automaticamente N transações individuais agrupadas por um `InstallmentGroup`
29. Cada parcela deve ter: número da parcela, total de parcelas, `CompetenceDate` e `DueDate` incrementados mensalmente
30. Não é permitido editar uma parcela isolada — alterações afetam o grupo inteiro via Ajuste
31. O cancelamento de uma parcela individual é permitido apenas se o status for `Pending`; parcelas com status `Paid` **não podem ser canceladas** — apenas corrigidas via Ajuste (F4)
32. O cancelamento do grupo cancela todas as parcelas com status `Pending`; parcelas já `Paid` permanecem inalteradas

### F7 — Recorrência Mensal

**Requisitos funcionais:**

32. A recorrência é definida por um template (registro de recorrência) que armazena a configuração da transação repetitiva
33. A geração de transações recorrentes é **lazy** — o sistema gera apenas a próxima ocorrência (1 mês à frente) quando o mês atual é consumido ou sob demanda na consulta de projeção. Nunca pré-gerar múltiplos meses de uma vez
34. A recorrência é mensal, sem data de fim no MVP; pode ser cancelada manualmente pelo usuário
35. Transações geradas por recorrência devem ser identificáveis como tal (flag e referência ao template de recorrência)

### F8 — Transferência entre Contas

**Requisitos funcionais:**

36. Uma transferência gera duas transações vinculadas: `Debit` na conta de origem e `Credit` na conta de destino
37. As duas transações devem compartilhar um identificador de transferência (`TransferGroup`)
38. Cancelar uma transferência deve cancelar ambas as transações e reverter os saldos
39. Transferências devem respeitar a regra de saldo negativo da conta de origem

### F9 — Auditoria Básica

**Requisitos funcionais:**

40. Toda entidade (conta, categoria, transação) deve registrar: `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`
41. Transações devem indicar: se é ajuste, se está cancelada, se é parcela, se é recorrente, se é transferência
42. O histórico de auditoria deve ser imutável — registros anteriores não podem ser alterados

### F10 — Concorrência e Idempotência

Requisitos técnicos de integridade para operações concorrentes.

**Requisitos funcionais:**

43. Operações que alteram saldo de conta devem usar **row-level locking** na conta alvo para evitar race conditions
44. Toda operação de escrita (criar transação, ajuste, cancelamento, transferência) deve executar dentro de uma **transação de banco de dados isolada** — falha parcial deve resultar em rollback completo
45. O sistema deve suportar um campo opcional `OperationId` (idempotency key) em operações de escrita para prevenir duplicidade em chamadas repetidas pela API futura
46. Se uma operação com `OperationId` já existente for recebida, o sistema deve retornar o resultado da operação original sem executar novamente

### F11 — Considerações sobre Tipos de Conta

47. O tipo `Investimento` é tratado como conta normal no Core — sem lógica especial de rendimento, aporte ou resgate. Aportes e resgates são transações comuns (`Credit` para aporte, `Debit` para resgate). Lógica especializada de investimento fica fora do escopo do MVP

## Experiência do Usuário

Nesta fase não há interface de usuário. O domínio será consumido pela camada de API (Fase 2) e interface (Fase 3).

**Personas:**
- **Membro da família**: registra transações, consulta saldos, gerencia contas
- **Admin da família**: mesmas permissões + cadastra membros da família

## Restrições Técnicas de Alto Nível

- **Stack**: .NET (C#) — camada de domínio e persistência
- **Banco de dados**: PostgreSQL
- **ORM**: Entity Framework Core
- **Saldo materializado**: o saldo da conta é um campo persistido, atualizado incrementalmente — nunca recalculado a partir do histórico
- **Imutabilidade**: transações nunca são alteradas após criação; correções via Adjustment (diferença contábil)
- **Sem exclusão física**: todas as entidades usam soft-delete ou status lógico
- **Auditoria obrigatória**: campos `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt` em todas as entidades
- **Concorrência**: row-level locking em contas para operações de saldo; transações de banco isoladas
- **Idempotência**: suporte a `OperationId` para prevenir duplicidade em operações de escrita
- **Overdue on-the-fly**: status `Overdue` é condição calculada em leitura, não persistida
- **Testes**: cobertura ≥ 90% no domínio; testes unitários determinísticos

## Não-Objetivos (Fora de Escopo)

- API HTTP / endpoints REST (→ Fase 2)
- Autenticação e autorização (→ Fase 2)
- Interface de usuário (→ Fase 3)
- Projeção financeira (→ Fase 4)
- Dashboard e gráficos (→ Fase 3)
- Múltiplas moedas
- Controle de orçamento por categoria
- Notificações
- Anexo de comprovantes
- Backup/export (→ Fase 2)
- Recorrência com padrões complexos (semanal, quinzenal, anual)
- Limite automático de cartão de crédito

## Decisões Tomadas (ex-Questões em Aberto)

1. **Recorrência sem fim** — **Decisão: geração lazy.** O sistema gera apenas 1 mês à frente sob demanda. Nunca pré-gera múltiplos meses.
2. **Ajuste em parcela** — **Decisão: ajuste por diferença, sem alterar parcelas pagas.** O ajuste cria transação compensatória (diferença). Parcelas já `Paid` permanecem inalteradas. Apenas parcelas futuras (`Pending`) podem ser recalculadas.
3. **Status Overdue** — **Decisão: calculado on-the-fly.** `Overdue` é condição de leitura (`DueDate < hoje AND status == Pending`), não status persistido no banco. Sem necessidade de job periódico.
4. **Conta Cartão — fatura** — **Decisão: transferência manual no MVP.** Pagamento de fatura é feito via transferência entre contas (cartão → corrente). Conceito de "fechamento de fatura" fica para v1.2+.

## Decisões Adicionais (ex-Questões em Aberto)

5. **Ajuste em grupo de parcelas** — **Decisão: divisão igualitária com arredondamento financeiro.** A diferença é dividida igualmente entre as parcelas futuras (`Pending`), com arredondamento para 2 casas decimais. O resíduo de arredondamento é aplicado na última parcela. Exemplo: diferença de +R$ 10,00 com 3 parcelas futuras = R$ 3,33 + R$ 3,33 + R$ 3,34. Isso garante total correto, histórico íntegro e complexidade baixa.
6. **Concorrência** — **Decisão: pessimista via `SELECT FOR UPDATE`.** Lock apenas na linha da conta alvo, dentro de transação ACID curta. Evita retries e garante consistência imediata no cenário de uso familiar (baixa concorrência, alta criticidade de correção).
7. **OperationId — TTL** — **Decisão: 24 horas.** Campo indexado no banco. Cleanup via job diário que remove registros com mais de 24h de idade.
