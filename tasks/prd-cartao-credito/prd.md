# PRD — Cartão de Crédito (Evolução do Modelo de Contas)

## Visão Geral

Evolução do modelo de contas do GestorFinanceiro para tratar o tipo `Cartão` de forma diferenciada dos demais tipos (Corrente, Investimento, Carteira). Atualmente, todos os tipos de conta compartilham a mesma estrutura — o cartão de crédito é cadastrado com "saldo inicial" ao invés de "limite de crédito" e não possui conceito de fechamento, vencimento, fatura ou conta de débito vinculada.

**Problema**: O cadastro de cartão de crédito é semanticamente incorreto — pede saldo inicial (irrelevante para cartão), não pede limite de crédito (essencial), e não modela o ciclo de fatura (fechamento → vencimento → pagamento). Isso foi uma decisão consciente do MVP (PRD Core Financeiro, Decisão 4), e agora é o momento de evoluir.

**Para quem**: Membros da família que usam cartões de crédito e precisam acompanhar limite disponível, fatura mensal e data de vencimento.

**Valor**: Modelo correto de cartão de crédito permite controle real de gastos no cartão, visualização de fatura por período de fechamento, e pagamento de fatura como operação dedicada — eliminando a necessidade de transferências manuais como workaround.

## Objetivos

- Diferenciar o cadastro de cartão de crédito dos demais tipos de conta (campos específicos: limite, fechamento, vencimento, conta de débito)
- Implementar conceito de fatura mensal com agrupamento de transações por ciclo de fechamento
- Permitir pagamento de fatura como operação dedicada (débito na conta vinculada, crédito no cartão)
- Exibir limite disponível e fatura atual no frontend
- Validar limite de crédito configurável por cartão (hard limit ou informativo)
- Manter retrocompatibilidade — contas existentes dos tipos Corrente, Investimento e Carteira não são afetadas

## Histórias de Usuário

- Como **membro da família**, quero cadastrar um cartão de crédito informando limite, dia de fechamento, dia de vencimento e conta de débito, para que o sistema modele corretamente meu cartão
- Como **membro da família**, quero ver o limite disponível do meu cartão (limite total − fatura atual) para saber quanto posso gastar
- Como **membro da família**, quero ver a fatura do mês agrupando todas as transações entre as datas de fechamento, para saber quanto vou pagar
- Como **membro da família**, quero pagar a fatura do cartão como uma operação dedicada, para que o sistema debite automaticamente a conta vinculada e credite o cartão
- Como **membro da família**, quero que o sistema me avise quando uma compra exceder o limite disponível (se configurado como limite rígido), para evitar gastar além do limite
- Como **membro da família**, quero editar o limite, dia de fechamento, dia de vencimento e conta de débito do meu cartão, para refletir mudanças no cartão real
- Como **membro da família**, quero que o formulário de cadastro se adapte quando eu selecionar "Cartão de Crédito", mostrando campos específicos ao invés de saldo inicial

## Funcionalidades Principais

### F1 — Cadastro Diferenciado de Cartão de Crédito

O cadastro de conta deve se adaptar quando o tipo selecionado for `Cartão`, exibindo campos específicos e ocultando campos irrelevantes.

**Requisitos funcionais:**

1. Ao criar uma conta do tipo `Cartão`, o sistema deve exigir: nome, limite de crédito, dia de fechamento (1-28), dia de vencimento (1-28) e conta de débito (obrigatória, mas pode ser alterada depois)
2. Cartão de crédito **não** possui campo "saldo inicial" — o saldo sempre inicia em 0 (zero)
3. Cartão de crédito **não** possui campo "permitir saldo negativo" — saldo negativo é sempre permitido implicitamente (compras geram saldo negativo, que representa a fatura)
4. O limite de crédito deve ser um valor maior que zero
5. Os dias de fechamento e vencimento devem estar no intervalo de 1 a 28 (para evitar problemas com meses de 28/29/30/31 dias)
6. A conta de débito vinculada deve ser uma conta ativa do tipo `Corrente` ou `Carteira`
7. Os demais tipos de conta (Corrente, Investimento, Carteira) mantêm o cadastro atual inalterado

### F2 — Edição de Cartão de Crédito

Permitir alterar propriedades específicas do cartão de crédito após a criação.

**Requisitos funcionais:**

8. O sistema deve permitir editar: nome, limite de crédito, dia de fechamento, dia de vencimento, conta de débito e flag de limite rígido
9. O tipo da conta (Cartão) não pode ser alterado após a criação
10. Alterações no dia de fechamento afetam apenas faturas futuras — a fatura do ciclo atual permanece com o fechamento original
11. Toda edição deve registrar usuário responsável e timestamp (auditoria)

### F3 — Validação de Limite de Crédito

Controle configurável de gastos além do limite do cartão.

**Requisitos funcionais:**

12. Cada cartão de crédito possui uma flag `EnforceCreditLimit` (padrão: true) que define se o limite é rígido ou informativo
13. Se `EnforceCreditLimit` = true: transações de débito (compras) que excedam o limite disponível devem ser rejeitadas com erro claro
14. Se `EnforceCreditLimit` = false: transações de débito são aceitas independente do limite, mas o frontend deve exibir alerta visual quando o limite for ultrapassado
15. O limite disponível é calculado como: `LimiteCredito - |SaldoAtual|` (onde saldo é ≤ 0 para cartão com fatura aberta)
16. Créditos (pagamentos de fatura) nunca são bloqueados pela validação de limite

### F4 — Fatura Mensal

Agrupamento de transações do cartão por ciclo de fechamento, permitindo visualização e pagamento.

**Requisitos funcionais:**

17. A fatura de um cartão agrupa todas as transações com status `Paid` realizadas entre o dia de fechamento do mês anterior e o dia de fechamento do mês atual
18. O sistema deve calcular o valor total da fatura (soma dos débitos − soma dos créditos no período)
19. O sistema deve disponibilizar a fatura do mês atual (aberta) e das faturas de meses anteriores (fechadas)
20. A fatura aberta atualiza automaticamente conforme novas transações são registradas
21. Faturas de meses anteriores são somente leitura — representam histórico
22. Transações de compras parceladas devem exibir a informação de parcela (ex: "Parcela 3/12") na listagem da fatura, permitindo ao usuário saber quantas parcelas restam sem precisar navegar mês a mês
23. Não há fechamento retroativo — faturas são calculadas apenas a partir da data de criação do cartão no sistema

### F5 — Pagamento de Fatura

Operação dedicada que substitui a transferência manual entre cartão e conta corrente.

**Requisitos funcionais:**

24. O pagamento de fatura gera duas transações vinculadas: `Debit` na conta de débito e `Credit` no cartão, similarmente a uma transferência
25. O pagamento deve informar: cartão, valor do pagamento e data de competência
26. A conta de débito utilizada é a conta vinculada no cadastro do cartão; o usuário pode alterar a conta de débito a qualquer momento na edição do cartão
27. O sistema deve permitir pagamento parcial da fatura (valor menor que o total)
28. O sistema deve permitir pagamento total da fatura (atalho que preenche automaticamente o valor total)
29. O sistema deve permitir pagamento acima do valor da fatura — o valor excedente gera saldo positivo (crédito a favor) no cartão que é abatido automaticamente na fatura seguinte
30. O pagamento deve respeitar as regras de saldo da conta de débito (validação de saldo negativo se aplicável)
31. O pagamento de fatura deve ser registrado como transferência específica (com `TransferGroup` e indicação de que é "pagamento de fatura")

### F6 — Exibição Diferenciada no Frontend

O card de conta e o formulário devem se adaptar para cartões de crédito.

**Requisitos funcionais:**

32. O card de cartão de crédito deve exibir: nome, fatura atual (valor gasto no ciclo), limite total, limite disponível, dia de fechamento e dia de vencimento
33. O label principal deve ser "Fatura Atual" (ao invés de "Saldo Atual") para cartões de crédito
34. Se o cartão possuir saldo positivo (crédito a favor), o card deve exibir "Crédito disponível: R$ X" de forma destacada
35. Deve haver indicação visual quando o limite disponível estiver abaixo de 20% do limite total (alerta amarelo) ou esgotado/ultrapassado (alerta vermelho)
36. O formulário de criação de conta deve alterar dinamicamente os campos exibidos quando o tipo selecionado for "Cartão de Crédito"
37. O formulário de edição de cartão deve exibir os campos específicos: limite, dia de fechamento, dia de vencimento, conta de débito, flag de limite rígido
38. A página de contas deve permitir acessar a fatura detalhada de um cartão (lista de transações do ciclo atual)
39. Transações parceladas na fatura devem exibir "Parcela X/Y" (ex: "Parcela 3/12 — TV Samsung") para que o usuário saiba quantas parcelas restam

### F7 — Dashboard — Cartão de Crédito

Evolução do card de dívida de cartão no dashboard.

**Requisitos funcionais:**

40. O card "Dívida de cartão total" deve continuar exibindo a soma das faturas de todos os cartões
41. Adicionalmente, o dashboard deve exibir: limite total agregado de todos os cartões e percentual de utilização
42. Ao clicar no card de dívida de cartão, o usuário deve ser direcionado à página de contas filtrada por tipo `Cartão`
43. O "Saldo Total" do dashboard **não deve incluir** o limite disponível dos cartões — limite de crédito não é dinheiro do usuário. Apenas o saldo negativo dos cartões (fatura/dívida) deve ser subtraído do saldo total como passivo

## Experiência do Usuário

**Personas:**
- **Membro da família**: cadastra cartões de crédito, registra compras, paga faturas
- **Admin da família**: mesmas permissões + gerencia configurações dos cartões (limite rígido, conta de débito padrão)

**Fluxos principais:**

1. **Cadastro do cartão**: Seleciona tipo "Cartão de Crédito" → formulário adapta para campos específicos → preenche limite, fechamento, vencimento → confirma criação
2. **Compra no cartão**: Cria transação de débito na conta do cartão → saldo do cartão fica negativo (fatura aumenta) → limite disponível diminui
3. **Visualização de fatura**: Abre card do cartão → vê fatura atual com lista de transações do ciclo → vê limite disponível
4. **Pagamento de fatura**: Clica "Pagar Fatura" no card do cartão → escolhe valor (total ou parcial) → confirma → sistema debita conta vinculada e credita cartão

**Considerações de UI/UX:**
- O formulário deve ter transição suave ao trocar o tipo de conta (campos aparecem/desaparecem com animação)
- O card do cartão deve ser visualmente diferenciado (já existe: cor roxa, ícone `credit_card`)
- A fatura deve ser exibida em formato de lista com subtotais
- Alertas de limite devem ser não-intrusivos (badges, cores) e não modais

## Restrições Técnicas de Alto Nível

- **Stack existente**: .NET (C#) no backend, React + TypeScript no frontend
- **Banco de dados**: PostgreSQL — evolução da tabela `accounts` com colunas adicionais nullable
- **Retrocompatibilidade**: Contas existentes dos tipos Corrente, Investimento e Carteira não devem ser afetadas
- **Saldo materializado**: O modelo de saldo materializado permanece — o saldo do cartão continua sendo atualizado incrementalmente por transação
- **Fatura é calculada, não materializada**: A fatura é uma consulta agregada por período de fechamento, não uma entidade persistida separada
- **Limite não é patrimônio**: O limite de crédito do cartão não deve ser contabilizado como patrimônio/dinheiro do usuário em nenhum cálculo agregado (saldo total, patrimônio líquido, etc.)
- **Sem migração de dados**: O sistema está em desenvolvimento — não há necessidade de migrar cartões existentes
- **Auditoria**: Todas as operações novas (pagamento de fatura, edição de cartão) devem seguir o padrão de auditoria existente
- **Concorrência**: Pagamento de fatura deve respeitar o row-level locking existente nas contas envolvidas
- **Testes**: Cobertura ≥ 90% para novas regras de domínio; testes unitários para validação de limite e cálculo de fatura

## Não-Objetivos (Fora de Escopo)

- Fatura mínima ou juros por atraso — não há cálculo de encargos por pagamento parcial
- Programa de pontos ou cashback
- Integração com bancos/operadoras de cartão (OFX, Open Finance)
- Notificações automáticas de vencimento de fatura (pode ser escopo futuro)
- Cartão de débito como tipo separado — cartão de débito é funcionalmente uma conta corrente
- Anuidade do cartão como transação automática
- Múltiplas datas de vencimento para o mesmo cartão (cartão adicional)
- Fatura em moeda estrangeira / conversão cambial
- Parcelamento de fatura ("parcelar fatura no cartão")
- Crédito rotativo

## Decisões Tomadas (ex-Questões em Aberto)

1. **Fechamento retroativo** — **Decisão: não implementar.** Faturas são calculadas apenas a partir da data de criação do cartão no sistema. Transações anteriores à criação do cartão não são agrupadas em faturas retroativas.
2. **Parcelamento na fatura** — **Decisão: exibir parcela X/Y.** Transações parceladas na listagem da fatura exibem "Parcela X/Y" (ex: "3/12"), facilitando o acompanhamento de quantas parcelas restam sem navegar mês a mês. Cada parcela já é uma transação individual com data de competência distinta, então cai naturalmente na fatura do mês correto.
3. **Conta de débito** — **Decisão: obrigatória no cadastro, alterável depois.** A conta de débito (Corrente ou Carteira) é obrigatória na criação do cartão, mas pode ser alterada a qualquer momento na edição.
4. **Saldo positivo (crédito excedente)** — **Decisão: permitir e abater da próxima fatura.** Se o usuário pagar mais que a fatura, o valor excedente gera saldo positivo no cartão. Esse crédito é abatido automaticamente do total da fatura seguinte. Por exemplo: fatura de R$ 500, pagamento de R$ 600 → saldo +R$ 100 no cartão → próxima fatura de R$ 400 → valor a pagar = R$ 300 (R$ 400 − R$ 100 de crédito).
5. **Limite não é patrimônio** — **Decisão: não contabilizar.** O limite de crédito do cartão nunca é somado ao saldo total ou patrimônio do usuário. Apenas a dívida (saldo negativo) é considerada como passivo nos cálculos agregados do dashboard.
