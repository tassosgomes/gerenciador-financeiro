# PRD — Orçamentos

## Visão Geral

A feature de Orçamentos adiciona uma camada de planejamento financeiro ao GestorFinanceiro, permitindo que o usuário defina envelopes de gasto (ex.: Moradia, Lazer, Alimentação) com um **percentual da renda mensal** e associe a cada envelope uma ou mais categorias de despesa já cadastradas no sistema. O valor limite é calculado automaticamente com base no percentual informado e na renda do mês (soma das receitas). A soma dos percentuais de todos os orçamentos não pode ultrapassar 100%. O saldo consumido é calculado de forma consolidada com base em todas as transações das categorias vinculadas.

**Problema**: Atualmente o sistema registra receitas e despesas, mas não oferece controle proativo. O usuário só percebe que gastou demais após o fato. Além disso, gastos de natureza similar (ex.: cinema, streaming, esportes) ficam pulverizados em categorias separadas sem visão consolidada.

**Para quem**: Usuários com renda fixa que querem saber, no momento de cada gasto, se ainda têm margem no orçamento daquele mês. Também atende usuários com dívidas ativas que precisam proteger um envelope dedicado à quitação.

**Valor**: Transforma dados históricos de transações em alertas visuais preventivos e metas claras, agrupando categorias relacionadas em um único envelope de controle mensal.

## Objetivos

- Permitir que o usuário planeje seus gastos mensais por envelopes temáticos antes das despesas acontecerem
- Reduzir a ocorrência de estouros de orçamento por meio de indicadores visuais de consumo
- Garantir que a soma dos percentuais de todos os orçamentos do mês não ultrapasse 100% da renda mensal do usuário
- Consolidar gastos de múltiplas categorias em uma única visão de controle
- Manter histórico de orçamentos para acompanhamento da evolução financeira mês a mês
- Atingir ≥ 60% dos usuários ativos com ao menos um orçamento configurado em 90 dias após lançamento
- Atingir ≥ 80% das transações de despesa associadas a algum orçamento em 90 dias

## Histórias de Usuário

- Como **membro da família**, quero criar um orçamento com nome e percentual da renda e associar a ele as categorias de despesa que desejar, para ter controle consolidado de gastos relacionados
- Como **membro da família**, quero que a soma dos percentuais de todos os meus orçamentos no mês não ultrapasse 100%, para garantir que estou distribuindo minha renda de forma coerente
- Como **membro da família**, quero ver o valor limite calculado automaticamente a partir do percentual e da minha renda mensal, para não precisar fazer cálculos manuais
- Como **membro da família**, quero que uma categoria de despesa possa pertencer a apenas um orçamento por mês, para evitar dupla contagem de gastos
- Como **membro da família**, quero ver o percentual consumido de cada orçamento com barra de progresso visual, para saber minha situação financeira de relance
- Como **membro da família**, quero receber indicação visual quando atingir 80% de um orçamento, para agir antes de estourar
- Como **membro da família**, quero ver um painel geral com todos os orçamentos do mês para ter visão macro dos gastos planejados vs. realizados
- Como **membro da família**, quero editar nome, percentual e categorias vinculadas de um orçamento, para ajustar conforme minha realidade muda
- Como **membro da família**, quero que transações de despesa em categorias vinculadas incrementem automaticamente o saldo do orçamento correspondente, sem exigir trabalho manual
- Como **membro da família**, quero consultar orçamentos de meses anteriores para comparar minha evolução financeira
- Como **membro da família**, quero replicar um orçamento (com suas categorias) para meses futuros usando recorrência, para não precisar recriar todo mês

## Funcionalidades Principais

### F1 — CRUD de Orçamentos

Gerenciamento de envelopes de gasto mensais com associação a categorias de despesa.

**Requisitos funcionais:**

1. O sistema deve permitir criar um orçamento informando: **nome** (texto livre), **percentual da renda** (decimal > 0, ≤ 100), **mês de referência** (formato ano-mês) e **categorias associadas** (seleção múltipla, **obrigatória — mínimo 1 categoria**)
2. A **renda mensal** do usuário é calculada automaticamente como a soma do `Amount` de todas as transações do tipo `Credit` com status `Paid` cuja `CompetenceDate` pertence ao mês de referência
3. O **valor limite** do orçamento é calculado automaticamente: `valor_limite = renda_mensal × (percentual / 100)` — o usuário informa apenas o percentual e o sistema calcula e exibe o valor correspondente em tempo real
4. A **soma dos percentuais** de todos os orçamentos de um mês de referência **não pode ultrapassar 100%** — o sistema deve bloquear a criação ou edição e informar o percentual restante disponível para orçar
5. O mês de referência deve ser o mês corrente ou qualquer mês futuro; meses passados não são permitidos para criação
6. Apenas categorias do tipo `Despesa` devem estar disponíveis para associação; categorias do tipo `Receita` são excluídas da seleção
7. Uma categoria de despesa só pode estar associada a **um único orçamento por mês de referência** — o sistema deve impedir duplicidade e sinalizar quais categorias já estão em uso
8. Um orçamento **deve obrigatoriamente ter ao menos uma categoria vinculada** — o sistema deve impedir criação ou edição que resulte em orçamento sem categorias
9. Ao excluir/migrar uma categoria que está vinculada a um orçamento, o sistema deve **desassociar automaticamente** a categoria do orçamento. Se o orçamento ficar sem nenhuma categoria após a desassociação, o sistema deve sinalizar ao usuário que o orçamento precisa de novas categorias
10. O sistema deve permitir editar: nome, percentual e categorias vinculadas de um orçamento do mês corrente ou futuro (respeitando o teto de 100%)
11. O sistema deve permitir excluir um orçamento do mês corrente ou futuro com confirmação explícita do usuário
12. Não há limite máximo de orçamentos por mês — o usuário pode criar quantos orçamentos desejar, desde que a soma dos percentuais não ultrapasse 100%
13. Toda criação/alteração de orçamento deve registrar: usuário responsável e timestamp (auditoria padrão `BaseEntity`)

### F2 — Dashboard de Orçamentos

Painel consolidado com visão de todos os orçamentos do mês.

**Requisitos funcionais:**

14. O dashboard deve exibir cards individuais para cada orçamento do mês selecionado
15. Cada card deve conter: nome do orçamento, percentual da renda atribuído, lista de categorias vinculadas, valor gasto (consumido), valor limite (calculado), valor restante e barra de progresso
16. A barra de progresso deve variar de cor conforme o percentual de consumo:
    - **Verde** — abaixo de 70% consumido
    - **Amarelo** — entre 70% e 89% consumido
    - **Vermelho** — 90% ou mais consumido
17. Ao ultrapassar 100% do limite, o card deve entrar em estado **"Estourado"** com destaque visual claro (badge/ícone)
18. Ao atingir 80% do limite, o card deve exibir indicação visual de alerta iminente (ícone de aviso)
19. O topo do dashboard deve exibir um resumo consolidado: **renda mensal** (soma das receitas), **total orçado** (soma dos limites / soma dos %), **total gasto** (soma dos consumidos), **saldo restante** e **renda não orçada** (% e valor da renda sem orçamento) no mês
20. Transações de categorias de despesa que **não estão vinculadas a nenhum orçamento** devem ser sinalizadas no dashboard como "gastos fora de orçamento", com valor total acumulado
21. O dashboard deve permitir selecionar o mês de visualização via filtro (mês/ano)

### F3 — Associação Automática de Transações

Cálculo do saldo consumido de cada orçamento com base nas transações das categorias vinculadas.

**Requisitos funcionais:**

22. Toda transação do tipo `Debit` (despesa) lançada com uma categoria vinculada a um orçamento deve incrementar automaticamente o saldo consumido daquele orçamento no mês de competência (`CompetenceDate`)
23. O saldo consumido de um orçamento é a **soma do `Amount` de todas as transações** do tipo `Debit` com status `Paid` cujas categorias estão vinculadas ao orçamento e cuja `CompetenceDate` pertence ao mês de referência
24. Transações do tipo `Credit` (receita) **não afetam** o saldo consumido de orçamentos, mas **afetam a renda mensal** e consequentemente o valor limite calculado de cada orçamento
25. Transações com status `Cancelled` **não afetam** o saldo de orçamentos nem a renda mensal
26. Ao cancelar uma transação que afetava um orçamento, o saldo consumido deve ser decrementado automaticamente
27. Ao criar um ajuste (Adjustment) em uma transação que afeta um orçamento, a diferença deve impactar o saldo do orçamento correspondente
28. Quando a renda mensal muda (nova receita ou cancelamento/ajuste de receita), o valor limite de todos os orçamentos do mês é recalculado automaticamente com base nos percentuais fixos
29. Transações sem categoria devem gerar aviso ao usuário para categorizar

### F4 — Histórico de Orçamentos

Consulta de orçamentos de meses encerrados para acompanhamento de evolução.

**Requisitos funcionais:**

30. O usuário deve poder consultar orçamentos de meses anteriores por meio de filtro de período (mês/ano)
31. Orçamentos de meses passados devem ser exibidos em modo **somente leitura** — não é permitido editar nem excluir orçamentos de meses já encerrados
32. O histórico deve exibir o consolidado final de cada orçamento: percentual, valor limite, valor gasto, percentual consumido e status final (dentro/estourado)

### F5 — Orçamento Recorrente

Replicação automática de orçamento para meses futuros.

**Requisitos funcionais:**

33. Ao criar ou editar um orçamento, o usuário deve poder marcar a opção **"Recorrente"**
34. Um orçamento recorrente é automaticamente replicado para o mês seguinte (com mesmo nome, percentual e categorias vinculadas) quando o mês de referência atual se encerra
35. A geração de orçamentos recorrentes é **lazy** — o sistema gera apenas o próximo mês, nunca pré-gera múltiplos meses
36. Se a soma dos percentuais dos orçamentos recorrentes ultrapassar 100% no novo mês, o sistema deve gerar os orçamentos mas sinalizar ao usuário que o total orçado excede 100%, solicitando ajuste
37. O usuário pode desativar a recorrência a qualquer momento; orçamentos já gerados permanecem inalterados
38. O usuário pode editar individualmente um orçamento gerado por recorrência sem afetar o template ou meses futuros

## Experiência do Usuário

**Personas:**
- **Planejador Consciente**: Pessoa com renda fixa que jorra anota gastos e quer controle ativo — saber no momento da compra se ainda tem espaço no orçamento do mês
- **Quitador de Dívidas**: Usuário com dívidas ativas que precisa destinar fatia fixa para quitação e proteger esse envelope de gastos do dia a dia

**Fluxo principal — Criação e Acompanhamento:**
1. Usuário acessa seção "Orçamentos" no menu principal
2. Clica em "Novo Orçamento", preenche nome e percentual da renda, seleciona mês de referência — o sistema exibe a renda mensal, o percentual já comprometido e o valor limite calculado em tempo real
3. Na etapa de categorias, o sistema exibe todas as categorias de despesa cadastradas — categorias já usadas em outro orçamento do mesmo mês aparecem desabilitadas com indicação clara
4. Usuário seleciona uma ou mais categorias (ex.: Lazer ← Esportes + Cinema + Streaming)
5. Card do orçamento aparece no dashboard com barra em verde (0% consumido)
6. Ao lançar despesa em qualquer categoria vinculada, o card é atualizado automaticamente
7. Ao atingir 80%, card exibe ícone de alerta e barra muda para amarelo
8. Ao ultrapassar 100%, barra fica vermelha e card exibe badge "Estourado"

**Fluxo secundário — Consulta Histórica:**
1. Usuário acessa o filtro de mês/ano no dashboard de orçamentos
2. Seleciona mês anterior
3. Visualiza orçamentos encerrados com consolidado final de cada um (somente leitura)

**Considerações de UI/UX:**
- Dashboard responsivo (mobile e desktop)
- Seleção múltipla de categorias com busca/filtro integrado
- Categorias indisponíveis (já em uso em outro orçamento) devem aparecer desabilitadas, não ocultas — para clareza
- Confirmação explícita ao excluir orçamento

**Acessibilidade:**
- Cores dos indicadores não devem ser o único canal de informação — usar ícones/textos complementares
- Labels e aria-labels em barras de progresso

## Restrições Técnicas de Alto Nível

- **Stack backend**: .NET (C#) — seguir mesma arquitetura de Domain, Application, Infra e Services do sistema existente
- **Stack frontend**: React + TypeScript + Tailwind CSS — seguir padrão de feature modules existente (`features/budgets/`)
- **Banco de dados**: PostgreSQL — constraint de unicidade `(categoria_id, mes_referencia)` na tabela de relacionamento para impedir duplicidade em nível de banco
- **Entidade base**: nova entidade `Budget` deve herdar de `BaseEntity` (auditoria padrão: `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`). O campo persistido é o **percentual** (`Percentage`); o valor limite é sempre calculado (`renda × percentual`)
- **Consistência**: cálculo do saldo consumido é a soma das transações `Debit` + `Paid` das categorias vinculadas no mês; cálculo da renda é a soma das transações `Credit` + `Paid` no mês; valor limite = renda × percentual — todos sem materialização (sempre calculados)
- **Performance**: consulta de saldo deve ser eficiente mesmo com grande volume de transações — índices apropriados nas colunas `CategoryId`, `CompetenceDate` e `Status` da tabela de transações
- **API REST**: endpoints sob `api/v1/budgets`, seguindo mesmo padrão CQRS existente com `IDispatcher`
- **Autenticação**: endpoints protegidos por `[Authorize]`, consistente com demais controllers

## Não-Objetivos (Fora de Escopo)

- Orçamentos compartilhados entre múltiplos grupos familiares
- Sugestão automática de valor limite baseada em IA ou histórico
- Integração com contas bancárias externas para importação de transações
- Projeção de gastos futuros com base em tendências
- Notificações push no navegador ou via email — alertas são apenas visuais no dashboard
- Orçamentos com período diferente de mensal (semanal, quinzenal, anual)
- Metas de economia por categoria
- Exportação de relatório de orçamentos em PDF/CSV (será incorporada à feature de backup/exportação futura)

## Decisões Tomadas

1. **Orçamento sem categorias** — **Decisão: não permitido.** Orçamento deve obrigatoriamente ter ao menos uma categoria vinculada. O propósito do orçamento é controlar o gasto consolidado de um conjunto de categorias.
2. **Exclusão de categoria em uso** — **Decisão: desassociação automática.** Ao excluir/migrar uma categoria vinculada a um orçamento, o sistema desassocia automaticamente. Se o orçamento ficar sem categorias, sinaliza ao usuário.
3. **Limite de orçamentos por mês** — **Decisão: sem limite.** O usuário pode criar quantos orçamentos desejar.
4. **Exportação PDF/CSV** — **Decisão: fora de escopo v1.** Funcionalidade será incorporada à feature de backup/exportação futura.
5. **Teto de renda** — **Decisão: orçamento baseado em percentual da renda.** O usuário informa um percentual (não valor fixo) e o sistema calcula o valor limite automaticamente (renda × %). A soma dos percentuais de todos os orçamentos não pode ultrapassar 100%. Renda mensal calculada da soma de transações `Credit` + `Paid` do mês. Criação/edição bloqueada se ultrapassar 100%. Orçamentos recorrentes gerados automaticamente são criados mesmo se ultrapassarem, com sinalização.

## Questões em Aberto

1. **Recorrência e mudança de categorias**: quando uma categoria é desativada (`IsActive = false`) após estar vinculada a um orçamento recorrente, o orçamento do mês seguinte deve ignorar essa categoria ou manter e sinalizar?
