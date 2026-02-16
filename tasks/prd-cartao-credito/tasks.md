```markdown
# Resumo de Tarefas de Implementação de Cartão de Crédito

## Visão Geral

Evolução do modelo de contas para diferenciar o tipo `Cartão` dos demais (Corrente, Investimento, Carteira). Implementa value object `CreditCardDetails` (composição 1:1 com `Account`), validação de limite de crédito, fatura mensal calculada por período de fechamento, pagamento de fatura como operação dedicada, e adaptações no frontend (formulário, card, drawer de fatura, dashboard).

## Fases de Implementação

### Fase 1 — Domain (Fundação)
Criação do value object `CreditCardDetails`, extensão da entidade `Account`, exceções específicas, `CreditCardDomainService` e integração com `TransactionDomainService`. Nenhuma dependência de infra — tudo testável unitariamente.

### Fase 2 — Infra (Persistência)
Migration para tabela `credit_card_details`, configuração EF Core `OwnsOne`, extensão de repositórios, seed de categoria "Pagamento de Fatura".

### Fase 3 — Application (Orquestração)
Commands e queries adaptados: criação/edição de cartão, consulta de fatura, pagamento de fatura. Validators com regras condicionais por tipo.

### Fase 4 — API (Exposição)
Adaptação de `AccountsController`, novo `InvoicesController`, novos DTOs de request/response.

### Fase 5 — Frontend (Interface)
Formulário dinâmico, card diferenciado, drawer de fatura, pagamento de fatura, dashboard estendido.

## Tarefas

- [x] 1.0 Value Object CreditCardDetails e Exceções
- [x] 2.0 Extensão da Entidade Account
- [x] 3.0 CreditCardDomainService e Validação de Limite
- [x] 4.0 Migration EF Core e Configuração de Persistência
- [x] 5.0 Extensão de Repositórios e Seed de Categoria
- [x] 6.0 Commands de Conta Adaptados para Cartão
- [x] 7.0 Query de Fatura Mensal
- [x] 8.0 Command de Pagamento de Fatura
- [x] 9.0 Endpoints API (Controllers e Requests)
- [x] 10.0 Frontend — Formulário e Tipos Adaptados
- [ ] 11.0 Frontend — Card de Cartão e Drawer de Fatura
- [ ] 12.0 Frontend — Dashboard Estendido

## Análise de Paralelização

### Lanes de Execução Paralela

| Lane | Tarefas | Descrição |
|------|---------|-----------|
| Lane A (Domain) | 1.0 → 2.0 → 3.0 | Caminho de domínio: value object → entidade → domain services |
| Lane B (Infra) | 4.0 → 5.0 | Persistência: migration → repositórios + seed |
| Lane C (Application) | 6.0, 7.0, 8.0 | Commands/queries — dependem de Lane A + Lane B, parcialmente paralelos |
| Lane D (API) | 9.0 | Controllers — depende de Lane C |
| Lane E (Frontend) | 10.0 → 11.0, 12.0 | Frontend: tipos/form → card/drawer + dashboard (paralelos entre si) |

### Caminho Crítico

```
1.0 → 2.0 → 3.0 → 4.0 → 5.0 → 6.0 → 9.0 → 10.0 → 11.0
```

O caminho mais longo passa por: value object → entidade → domain services → migration → repositórios → commands → API → frontend form → frontend card/drawer.

### Diagrama de Dependências

```
┌───────────────────────────────────────────────────────────────┐
│  FASE 1 — Domain                                              │
│                                                               │
│  ┌─────┐                                                      │
│  │ 1.0 │ CreditCardDetails + Exceções                        │
│  └──┬──┘                                                      │
│     │                                                         │
│  ┌──▼──┐                                                      │
│  │ 2.0 │ Extensão Account (CreditCard?, factory methods)      │
│  └──┬──┘                                                      │
│     │                                                         │
│  ┌──▼──┐                                                      │
│  │ 3.0 │ CreditCardDomainService + ValidateCreditLimit        │
│  └──┬──┘                                                      │
└─────┼─────────────────────────────────────────────────────────┘
      │
┌─────▼─────────────────────────────────────────────────────────┐
│  FASE 2 — Infra                                               │
│                                                               │
│  ┌─────┐                                                      │
│  │ 4.0 │ Migration + EF Config OwnsOne                        │
│  └──┬──┘                                                      │
│     │                                                         │
│  ┌──▼──┐                                                      │
│  │ 5.0 │ Repositórios + Seed Categoria                        │
│  └──┬──┘                                                      │
└─────┼─────────────────────────────────────────────────────────┘
      │
┌─────▼─────────────────────────────────────────────────────────┐
│  FASE 3 — Application                                         │
│                                                               │
│  ┌─────┐     ┌─────┐     ┌─────┐                             │
│  │ 6.0 │     │ 7.0 │     │ 8.0 │                             │
│  │Cmds │     │Query│     │Pgto │                             │
│  │Conta│     │Fatur│     │Fatur│                             │
│  └──┬──┘     └──┬──┘     └──┬──┘                             │
│     │           │           │  (6.0 e 7.0 são paralelos;      │
│     │           │           │   8.0 depende de 5.0)           │
└─────┼───────────┼───────────┼─────────────────────────────────┘
      │           │           │
┌─────▼───────────▼───────────▼─────────────────────────────────┐
│  FASE 4 — API                                                 │
│                                                               │
│  ┌─────┐                                                      │
│  │ 9.0 │ Controllers + Requests                               │
│  └──┬──┘                                                      │
└─────┼─────────────────────────────────────────────────────────┘
      │
┌─────▼─────────────────────────────────────────────────────────┐
│  FASE 5 — Frontend                                            │
│                                                               │
│  ┌──────┐                                                     │
│  │ 10.0 │ Tipos + Formulário                                  │
│  └──┬───┘                                                     │
│     │                                                         │
│  ┌──▼──┐     ┌──────┐                                        │
│  │11.0 │     │ 12.0 │  (paralelos)                           │
│  │Card │     │Dashb.│                                        │
│  │Drawr│     │      │                                        │
│  └─────┘     └──────┘                                        │
└───────────────────────────────────────────────────────────────┘
```
```
