# 📊 PRD — Feature: Orçamentos
**Sistema Financeiro Pessoal**

---

| Campo | Valor |
|---|---|
| Produto | Sistema Financeiro Pessoal |
| Feature | Orçamentos |
| Versão do PRD | 1.0 |
| Data | Fevereiro 2026 |
| Status | Proposta — Aguardando aprovação |
| Autor | Product Owner |

---

## 1. Visão Geral

Este documento descreve os requisitos de produto para a feature **Orçamentos**, a ser incorporada ao Sistema Financeiro Pessoal. O objetivo é fornecer ao usuário uma camada de planejamento financeiro que permita definir envelopes de gasto (ex: Lazer, Moradia, Transporte) e associar a cada envelope as **categorias já cadastradas no sistema** — permitindo que múltiplas categorias alimentem um único orçamento. O saldo consumido é calculado de forma consolidada com base em todas as categorias vinculadas.

> **🎯 Problema que resolve**
> Atualmente o sistema registra receitas e despesas, mas não oferece um mecanismo proativo de controle. O usuário só percebe que gastou demais após o fato. Além disso, gastos de natureza similar (ex: cinema, streaming, esportes) ficam pulverizados em categorias separadas sem visão consolidada. A feature de Orçamentos transforma dados históricos em alertas preventivos e metas claras, agrupando categorias relacionadas em um único envelope de controle.

---

## 2. Objetivos de Negócio

- Aumentar o engajamento semanal do usuário com o sistema financeiro.
- Reduzir a ocorrência de estouros de orçamento por categoria.
- Fortalecer o hábito de planejamento mensal antes dos gastos acontecerem.
- Diferenciar o sistema de simples anotadores financeiros ao oferecer inteligência de controle.

---

## 3. Público-Alvo e Personas

### Persona Principal — O Planejador Consciente

Pessoa com renda fixa mensal (CLT ou autônomo estável) que já tem o hábito de anotar gastos, mas sente que falta um mecanismo de controle ativo. Quer saber, no momento da compra, se ainda tem espaço no orçamento daquele mês.

- **Frustrações:** Descobre o estouro só no fim do mês; não tem visão de quanto ainda pode gastar.
- **Ganho esperado:** Alerta antes do estouro; painel visual simples de entender.

### Persona Secundária — O Quitador de Dívidas

Usuário com dívidas ativas que precisa destinar uma fatia fixa do salário para quitação e não comprometer o valor com gastos do dia a dia. Precisa garantir que categorias como lazer e vestuário não consumam o envelope destinado à quitação.

- **Frustrações:** Dinheiro destinado à dívida acaba sendo usado em outros gastos sem perceber.
- **Ganho esperado:** Orçamento dedicado à quitação visível e protegido; alertas de risco.

---

## 4. User Stories

| ID | User Story | Critério de Aceite |
|---|---|---|
| US-01 | Como usuário, quero criar um orçamento com nome e valor limite, e associar a ele as categorias que desejar, para ter controle consolidado de gastos relacionados. | Formulário com nome, valor limite, mês e seleção múltipla de categorias cadastradas. |
| US-02 | Como usuário, quero que uma categoria possa pertencer a apenas um orçamento por vez para evitar dupla contagem. | Sistema impede associar a mesma categoria a dois orçamentos no mesmo mês. |
| US-03 | Como usuário, quero ver o percentual consumido de cada orçamento para saber minha situação. | Barra de progresso visual com % e valor restante, consolidando todas as categorias vinculadas. |
| US-04 | Como usuário, quero receber alerta quando atingir 80% de um orçamento para agir antes de estourar. | Notificação/alerta automático ao atingir 80% do limite. |
| US-05 | Como usuário, quero ver um painel geral com todos os orçamentos do mês para ter visão macro. | Dashboard com todos os orçamentos, status e totais consolidados. |
| US-06 | Como usuário, quero editar as categorias associadas a um orçamento para ajustar conforme minha realidade muda. | Edição de nome, valor e categorias vinculadas; com confirmação para exclusão. |
| US-07 | Como usuário, quero ver o histórico de orçamentos de meses anteriores para comparar evolução. | Filtro por mês/ano com dados históricos preservados. |
| US-08 | Como usuário, quero que o sistema associe automaticamente as transações aos orçamentos para não ter trabalho manual. | Transação em categoria X incrementa o orçamento que contém a categoria X. |

---

## 5. Requisitos Funcionais

### 5.1 Criação de Orçamento

- O usuário deve poder criar um orçamento informando: **nome** (ex: "Lazer"), **valor limite**, **mês de referência** e **categorias associadas**.
- A seleção de categorias deve exibir todas as categorias já cadastradas no sistema, com busca e seleção múltipla.
- Uma categoria só pode estar associada a **um único orçamento por mês** — o sistema deve impedir duplicidade e sinalizar quais categorias já estão em uso.
- Um orçamento pode existir **sem categorias vinculadas** (controle manual), mas o sistema deve alertar que ele não receberá lançamentos automáticos.
- Deve haver opção de replicar o orçamento (com suas categorias) para meses futuros (recorrência).

### 5.2 Painel de Orçamentos (Dashboard)

- O dashboard deve exibir cards para cada orçamento do mês corrente.
- Cada card deve conter: nome, categoria, valor gasto, valor limite, valor restante e barra de progresso.
- A barra de progresso deve variar de cor conforme o consumo:
  - 🟢 **Verde** — abaixo de 70% consumido
  - 🟡 **Amarelo** — entre 70% e 89% consumido
  - 🔴 **Vermelho** — 90% ou mais consumido
- O topo do dashboard deve exibir um resumo consolidado: **total orçado vs. total gasto** no mês.

### 5.3 Associação Automática de Transações

- Toda transação lançada com uma categoria deve incrementar automaticamente o saldo do orçamento que contém aquela categoria no mês vigente.
- O cálculo do saldo consumido é a **soma de todas as transações** das categorias vinculadas ao orçamento.
- Transações de receita não devem afetar orçamentos.
- Transações em categorias **não vinculadas a nenhum orçamento** devem ser sinalizadas no dashboard como "fora do controle de orçamento", incentivando o usuário a organizar.
- Transações sem categoria devem gerar aviso ao usuário para categorizar.

### 5.4 Alertas

- Ao atingir **80%** do limite de um orçamento, o sistema deve emitir um alerta (notificação push e/ou indicador visual no dashboard).
- Ao ultrapassar **100%**, o card deve entrar em estado **"Estourado"** com destaque visual claro.

### 5.5 Histórico

- O usuário deve poder consultar orçamentos de meses anteriores com filtro por período.
- Dados históricos são **somente leitura** — não é possível editar orçamentos de meses já encerrados.

---

## 6. Modelo de Dados

A mudança central desta feature é a relação **1 orçamento → N categorias**, substituindo a relação anterior de 1 para 1.

```
Orçamento
├── id
├── nome              (ex: "Lazer")
├── valor_limite      (ex: 770.00)
├── mes_referencia    (ex: 2026-02)
├── recorrente        (boolean)
└── categorias[]      → FK para tabela de Categorias já existente

Categoria (já existente)
├── id
├── nome              (ex: "Esportes", "Streaming", "Cinema")
└── tipo              (despesa | receita)

Relacionamento
└── orcamento_categorias
    ├── orcamento_id
    └── categoria_id
    (unique constraint: categoria_id + mes_referencia → garante 1 categoria por orçamento/mês)
```

> **⚠️ Regra de integridade:** a unique constraint em `(categoria_id, mes_referencia)` deve ser aplicada no banco de dados, não apenas na interface — garantindo consistência mesmo em integrações futuras via API.

---

- **Performance:** A atualização do saldo de orçamento deve ser síncrona ao lançamento da transação (sem delay perceptível).
- **Consistência:** Exclusão de transação deve decrementar o saldo do orçamento correspondente.
- **Persistência:** Orçamentos e histórico devem ser armazenados de forma durável (não perdidos em atualizações do app).
- **Responsividade:** O dashboard deve ser utilizável tanto em mobile quanto em desktop.

---

## 7. Critérios de Aceite

| Critério | Prioridade | Detalhe |
|---|---|---|
| Criação de orçamento | Alta | Nome, valor limite, mês de referência e seleção múltipla de categorias |
| Validação de categoria única | Alta | Sistema impede a mesma categoria em dois orçamentos no mesmo mês |
| Barra de progresso | Alta | Visual com cores: verde (<70%), amarelo (70–89%), vermelho (≥90%) |
| Alerta de estouro iminente | Alta | Notificação ao atingir 80% do limite |
| Dashboard consolidado | Alta | Visão de todos os orçamentos do mês com total gasto vs. planejado |
| Associação automática | Alta | Transação em categoria X → orçamento que contém categoria X é atualizado |
| Categorias sem orçamento | Alta | Sinalizar no dashboard transações de categorias fora de qualquer orçamento |
| Editar / Excluir orçamento | Média | Edição de nome, valor e categorias vinculadas; confirmação para exclusão |
| Histórico de meses anteriores | Média | Filtro por período, dados somente leitura |
| Orçamento recorrente | Baixa | Replicar orçamento com suas categorias para meses futuros |
| Exportar relatório | Baixa | PDF ou CSV do resumo mensal |

---

## 8. Fora de Escopo (v1.0)

- Orçamentos compartilhados entre múltiplos usuários.
- Sugestão automática de valor limite baseada em IA.
- Integração com contas bancárias externas para importação de transações.
- Projeção de gastos futuros com base em tendências.

> **🔮 Backlog futuro (v2.0+)**
> Sugestão inteligente de limites com base no histórico dos últimos 3 meses, metas de economia por categoria, relatório comparativo mensal e exportação em PDF/CSV.

---

## 9. Fluxo de Usuário Simplificado

### Fluxo Principal — Criação e Acompanhamento

1. Usuário acessa seção **"Orçamentos"** no menu principal.
2. Clica em **"Novo Orçamento"**, preenche nome e valor limite.
3. Na etapa de categorias, o sistema exibe todas as categorias cadastradas — categorias já usadas em outro orçamento do mesmo mês aparecem desabilitadas.
4. Usuário seleciona uma ou mais categorias (ex: Lazer ← Esportes + Cinema + Streaming).
5. Card do orçamento aparece no dashboard com as categorias vinculadas e barra em verde (0% consumido).
6. Ao lançar despesa em qualquer categoria vinculada, o card é atualizado automaticamente.
7. Ao atingir 80%, o usuário recebe alerta e a barra muda para amarelo.
8. Ao ultrapassar 100%, barra fica vermelha e card exibe badge **"Estourado"**.

### Fluxo Secundário — Consulta Histórica

1. Usuário acessa **"Histórico de Orçamentos"**.
2. Seleciona mês/ano desejado no filtro.
3. Visualiza orçamentos encerrados com o consolidado final de cada um.

---

## 10. Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Transações sem categoria não associadas a orçamento | Alta | Médio | Alertar usuário para categorizar lançamento pendente |
| Usuário não criar orçamentos e feature ficar sem uso | Média | Alto | Onboarding guiado sugerindo orçamentos baseado nos gastos históricos |
| Orçamentos desatualizados em meses seguintes | Média | Médio | Sugestão de revisão mensal com notificação no início do mês |
| Performance com grande volume de transações | Baixa | Médio | Cálculo de saldo incremental ao lançar transação |

---

## 11. Métricas de Sucesso

| Métrica | Baseline (atual) | Meta em 90 dias |
|---|---|---|
| % usuários com ao menos 1 orçamento ativo | 0% | ≥ 60% |
| Transações associadas a orçamento | 0% | ≥ 80% |
| Usuários que acessam dashboard semanalmente | N/A | ≥ 40% |
| Taxa de estouro de orçamento evitado (alerta funcionou) | N/A | Medir após 30 dias |

A feature será considerada bem-sucedida se, em 90 dias após o lançamento, pelo menos 60% dos usuários ativos tiverem ao menos um orçamento configurado e 80% das transações estiverem associadas a algum orçamento.

---

## 12. Roadmap de Entrega Sugerido

### Sprint 1 — Fundação (semana 1–2)
- Model de dados: tabela de orçamentos com campos de categoria, limite, mês e saldo consumido.
- CRUD de orçamentos (criar, listar, editar, excluir).
- Associação automática ao lançar transação.

### Sprint 2 — Dashboard e Alertas (semana 3–4)
- Cards visuais com barra de progresso e código de cores.
- Painel consolidado com total orçado vs. gasto.
- Alertas de 80% e estado "Estourado".

### Sprint 3 — Histórico e Polimento (semana 5–6)
- Filtro de histórico por mês/ano.
- Opção de orçamento recorrente.
- Testes de usabilidade e ajustes de UX.

---

## 13. Aprovações

| Papel | Nome | Data de Aprovação |
|---|---|---|
| Product Owner | ___________________ | _____ / _____ / _______ |
| Tech Lead | ___________________ | _____ / _____ / _______ |
| Designer (UX) | ___________________ | _____ / _____ / _______ |

---

*Fim do documento  •  PRD Orçamentos v1.0*