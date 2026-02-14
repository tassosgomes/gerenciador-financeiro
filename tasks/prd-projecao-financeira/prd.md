# PRD — Projeção Financeira (Fase 4)

## Visão Geral

A Fase 4 adiciona a funcionalidade de projeção financeira ao GestorFinanceiro: um cálculo determinístico que mostra ao usuário o saldo projetado para os próximos 3 meses com base em transações futuras já registradas (parcelas, recorrências e despesas pendentes). Inclui um endpoint de API e uma tela dedicada no frontend.

**Problema**: Saber o saldo atual não é suficiente para tomar decisões financeiras. A família precisa visualizar compromissos futuros e saldo esperado para planejar gastos e evitar surpresas.

**Para quem**: Membros da família que querem antecipar a situação financeira futura sem depender de planilhas.

**Valor**: Visão clara de futuro financeiro baseada em dados reais (parcelas, recorrências, pendentes), sem complexidade de IA ou previsões estatísticas.

## Objetivos

- Fornecer projeção de saldo para os próximos 3 meses a partir da data atual
- Incluir todas as fontes de transações futuras: parcelas, recorrências e pendentes
- Manter o cálculo 100% determinístico — apenas matemática sobre dados existentes
- Entregar tela de projeção no frontend com visualização clara
- Não introduzir complexidade computacional significativa (sem jobs, sem cache adicional)

## Histórias de Usuário

- Como **membro da família**, quero ver meu saldo projetado para os próximos 3 meses para saber se vou ter dinheiro suficiente
- Como **membro da família**, quero ver quais parcelas vencem nos próximos meses para me preparar
- Como **membro da família**, quero ver o impacto das recorrências mensais no meu saldo futuro
- Como **membro da família**, quero ver despesas pendentes que ainda não paguei para priorizar pagamentos
- Como **membro da família**, quero ver a projeção por conta para entender a situação de cada uma

## Funcionalidades Principais

### F1 — Motor de Projeção (Backend)

**Requisitos funcionais:**

1. O cálculo de projeção deve partir do saldo atual de cada conta ativa
2. O horizonte de projeção é de 3 meses a partir da data atual (mês atual + 2 meses seguintes)
3. O cálculo deve considerar as seguintes fontes de transações futuras:
   - Parcelas futuras (`InstallmentGroup`) com status `Pending` e `DueDate` dentro do período
   - Transações recorrentes projetadas para os meses do período
   - Transações com status `Pending` e `DueDate` dentro do período
4. O cálculo é: `Saldo projetado = Saldo atual + Σ Credits futuros - Σ Debits futuros`
5. A projeção deve ser calculada mês a mês (saldo ao final de cada mês)
6. Transações `Cancelled` e `Paid` (já contabilizadas no saldo) não devem entrar na projeção
7. O cálculo não utiliza IA, machine learning ou previsões estatísticas — apenas aritmética sobre dados existentes

### F2 — Endpoint de Projeção

**Requisitos funcionais:**

8. `GET /api/projecao` — retornar projeção consolidada para os próximos 3 meses
9. `GET /api/projecao?contaId={id}` — projeção filtrada por conta específica
10. Resposta deve conter:
    - Saldo atual (total e por conta)
    - Para cada mês projetado: saldo projetado, total de receitas esperadas, total de despesas esperadas
    - Lista de parcelas futuras no período (com valor, descrição, data de vencimento, parcela X de Y)
    - Lista de recorrências no período (com valor, descrição, próxima data)
    - Lista de transações pendentes no período (com valor, descrição, data de vencimento)
11. A resposta deve estar ordenada cronologicamente
12. O endpoint requer autenticação JWT

### F3 — Tela de Projeção (Frontend)

**Requisitos funcionais:**

13. Nova seção "Projeção" acessível pelo menu de navegação
14. Exibir card de **saldo atual** no topo
15. Exibir **3 cards de saldo projetado** — um para cada mês (ex.: "Março 2026", "Abril 2026", "Maio 2026"), com valor e indicação visual (verde se positivo, vermelho se negativo)
16. Seção **Parcelas futuras**: tabela com descrição, valor, data de vencimento, parcela X/Y
17. Seção **Recorrências**: tabela com descrição, valor, próxima data
18. Seção **Despesas pendentes**: tabela com descrição, valor, data de vencimento, conta
19. Filtro por conta (dropdown para ver projeção de conta específica ou todas)
20. Valores em R$ com formatação brasileira
21. Indicação visual de "atenção" quando saldo projetado for negativo

## Experiência do Usuário

### Fluxo Principal

1. Usuário acessa "Projeção" no menu
2. Vê saldo atual e saldos projetados para 3 meses
3. Rola a página para ver detalhes (parcelas, recorrências, pendentes)
4. Opcionalmente filtra por conta específica

### Diretrizes de UI/UX

- Layout vertical: cards no topo, tabelas abaixo
- Cores semafóricas: verde (saldo positivo), vermelho (saldo negativo), amarelo (saldo baixo — abaixo de um limiar configurável ou 10% do saldo atual)
- Tabelas ordenadas por data de vencimento
- Sem gráficos complexos — números claros são suficientes nesta fase
- Loading skeleton enquanto a API responde

### Acessibilidade

- Cores acompanhadas de texto/ícones (não depender apenas de cor)
- Tabelas com cabeçalhos semânticos (`<th>`)
- Valores numéricos com `aria-label` descritivo

## Restrições Técnicas de Alto Nível

- **Cálculo server-side**: a projeção é calculada no backend, não no frontend
- **Sem cache extra**: o cálculo é feito on-demand a cada requisição (dados são poucos no cenário familiar)
- **Sem jobs**: não há processo periódico para gerar projeções — é sempre calculado em tempo real
- **Performance**: tempo de resposta < 2s para família com até 5.000 transações
- **Dependência**: requer Fase 1 (Core) e Fase 2 (API) implementadas

## Não-Objetivos (Fora de Escopo)

- Previsão baseada em IA ou machine learning
- Projeção além de 3 meses
- Cenários "e se" (what-if analysis)
- Alertas automáticos de saldo baixo (→ pós-MVP)
- Projeção de investimentos com rendimento
- Gráfico de evolução temporal de saldo projetado
- Comparativo com meses anteriores
- Meta de economia / orçamento por categoria

## Questões em Aberto

1. **Recorrências futuras**: como determinar o valor da recorrência no mês futuro se ela nunca teve ajuste? (sugestão: usar o valor da última ocorrência gerada)
2. **Saldo negativo projetado**: exibir apenas um alerta visual ou bloquear algo? (sugestão: apenas alerta visual, sem bloqueio)
3. **Granularidade**: a projeção deve mostrar saldo dia a dia ou apenas o total ao final de cada mês? (sugestão: total mensal no MVP)
4. **Conta cartão**: incluir fatura futura do cartão como despesa projetada?
