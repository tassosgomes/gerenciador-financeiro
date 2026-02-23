# Resumo de Tarefas de Implementação de Importação de Cupom Fiscal (NFC-e)

## Visão Geral

Implementação do recurso de Importação de Cupom Fiscal (NFC-e) para o GestorFinanceiro. O recurso permite que o usuário importe dados de uma NFC-e da SEFAZ PB, criando automaticamente uma transação de despesa e armazenando cada item individual do cupom. A solução envolve: novas entidades de domínio (`ReceiptItem`, `Establishment`), serviço de web scraping da SEFAZ PB, endpoints de API (lookup + import), e um wizard de importação no frontend.

## Fases de Implementação

### Fase 1 — Fundação Backend (Tasks 1-3)
Criação das entidades de domínio, interfaces, infraestrutura de persistência (EF Core, repositórios, migration) e serviço de scraping da SEFAZ PB. Tasks 2 e 3 podem ser executadas em paralelo após a conclusão da Task 1.

### Fase 2 — Lógica de Aplicação e API (Tasks 4-5)
Implementação dos command/query handlers na camada Application, DTOs de resposta, extensão do handler de cancelamento, e criação do controller com endpoints REST e tratamento de exceções.

### Fase 3 — Frontend (Tasks 6-7)
Criação dos tipos TypeScript, funções de API, hooks React Query, e implementação da página de importação (wizard 3 steps), componentes de preview/detalhe e integração com as telas existentes de transações.

## Tarefas

- [x] 1.0 Entidades de Domínio, DTOs e Interfaces ✅ CONCLUÍDA
- [x] 2.0 Infraestrutura — EF Core, Repositórios e Migration ✅ CONCLUÍDA
- [x] 3.0 Serviço SEFAZ PB — Scraping e Parsing
- [x] 4.0 Commands, Queries e Handlers (Application Layer)
- [x] 5.0 API — Controller, Requests e Exception Handling
- [ ] 6.0 Frontend — Tipos, API Client e Hooks
- [ ] 7.0 Frontend — Página de Importação e Integração UI

## Análise de Paralelização

### Lanes de Execução Paralela

| Lane | Tarefas | Descrição |
|------|---------|-----------|
| Lane A (Infra/Persistência) | 1.0 → 2.0 | Entidades de domínio → EF Core + Repositórios + Migration |
| Lane B (Integração SEFAZ) | 1.0 → 3.0 | Entidades de domínio → Serviço de scraping SEFAZ PB |
| Lane C (Application + API) | 4.0 → 5.0 | Handlers + DTOs → Controller + Exception Handling |
| Lane D (Frontend) | 6.0 → 7.0 | Tipos + API + Hooks → Página de importação + UI |

### Caminho Crítico

```
1.0 → 2.0 → 4.0 → 5.0 → 6.0 → 7.0
```

A Task 3.0 (SEFAZ scraping) pode ser desenvolvida em paralelo com a Task 2.0, mas ambas precisam estar concluídas antes da Task 4.0.

### Diagrama de Dependências

```
                    ┌──────┐
                    │ 1.0  │  Entidades de Domínio e Interfaces
                    └──┬───┘
                   ┌───┴───┐
                   │       │
              ┌────▼──┐ ┌──▼────┐
              │  2.0  │ │  3.0  │  (paralelas)
              │ Infra │ │ SEFAZ │
              └───┬───┘ └───┬───┘
                  └────┬────┘
                  ┌────▼────┐
                  │   4.0   │  Commands/Queries/Handlers
                  └────┬────┘
                  ┌────▼────┐
                  │   5.0   │  API Controller
                  └────┬────┘
                  ┌────▼────┐
                  │   6.0   │  Frontend Tipos/API/Hooks
                  └────┬────┘
                  ┌────▼────┐
                  │   7.0   │  Frontend Página/UI
                  └─────────┘
```
