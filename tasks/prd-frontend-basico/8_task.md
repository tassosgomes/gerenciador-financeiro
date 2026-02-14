---
status: pending
parallelizable: false
blocked_by: ["3.0", "4.0", "6.0", "7.0"]
---

<task_context>
<domain>frontend/transactions</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>tanstack-query, react-hook-form, zod, react-router-dom</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 8.0: CRUD de Transações

## Visão Geral

Implementar a feature mais complexa do sistema: gestão completa de transações. Inclui listagem paginada com filtros avançados (conta, categoria, tipo, status, período), formulário de criação com abas para 4 tipos (Simples, Parcelada, Recorrente, Transferência), preview de parcelas, ações de cancelamento e ajuste, e página de detalhe com histórico de auditoria. Os filtros devem ser sincronizados com a URL para permitir compartilhamento de links.

## Requisitos

- PRD F5 req. 26: Listagem com data de competência, descrição, categoria, conta, valor, status
- PRD F5 req. 27: Filtros por conta, categoria, tipo (Debit/Credit), status, período de competência
- PRD F5 req. 28: Paginação na listagem
- PRD F5 req. 29: Formulário de criação com abas: Simples, Parcelada, Recorrente, Transferência
- PRD F5 req. 30: Transação simples: conta, tipo, valor, categoria, data de competência, vencimento, descrição
- PRD F5 req. 31: Parcelada: mesmos campos + número de parcelas + preview antes de confirmar
- PRD F5 req. 32: Recorrente: mesmos campos + indicação de recorrência mensal
- PRD F5 req. 33: Transferência: conta origem, conta destino, valor, data de competência, descrição
- PRD F5 req. 34: Ação de cancelar com confirmação e campo de motivo
- PRD F5 req. 35: Ação de ajustar com novo valor e justificativa
- PRD F5 req. 36: Detalhe com dados completos, status, tipo, parcela X de Y, histórico de auditoria
- PRD F5 req. 37: Valores em R$ com cores verde (Credit) e vermelho (Debit)
- PRD F5 req. 38: Transações canceladas com indicação visual (riscado ou badge)
- Layout do formulário fiel ao mockup `screen-examples/nova-transacao/index.html`
- Layout da listagem fiel ao mockup `screen-examples/historico-financeiro/index.html`

## Subtarefas

### Tipos e API

- [ ] 8.1 Criar `src/features/transactions/types/transaction.ts` — enums `TransactionType`, `TransactionStatus`; interfaces `TransactionResponse`, `CreateTransactionRequest`, `CreateInstallmentRequest`, `CreateRecurrenceRequest`, `CreateTransferRequest`, `AdjustTransactionRequest`, `CancelTransactionRequest`, `TransactionHistoryEntry`
- [ ] 8.2 Criar `src/features/transactions/api/transactionsApi.ts` — funções: `getTransactions(filters)`, `getTransaction(id)`, `createTransaction(data)`, `createInstallment(data)`, `createRecurrence(data)`, `createTransfer(data)`, `adjustTransaction(id, data)`, `cancelTransaction(id, data)`, `getTransactionHistory(id)` usando apiClient

### Hooks

- [ ] 8.3 Criar `src/features/transactions/hooks/useTransactions.ts` — hooks TanStack Query: `useTransactions(filters)`, `useTransaction(id)`, `useTransactionHistory(id)`, `useCreateTransaction()`, `useCreateInstallment()`, `useCreateRecurrence()`, `useCreateTransfer()`, `useAdjustTransaction()`, `useCancelTransaction()` com mutations e invalidação de cache
- [ ] 8.4 Criar `src/features/transactions/hooks/useTransactionFilters.ts` — hook que sincroniza filtros com URL (query params via `useSearchParams`): accountId, categoryId, type, status, dateFrom, dateTo, page; funções `setFilter()`, `clearFilters()`, `toQueryString()`

### Componentes de Listagem

- [ ] 8.5 Criar `src/features/transactions/components/TransactionFilters.tsx` — barra de filtros: select de conta (lista de contas ativas), select de categoria, select de tipo (Débito/Crédito), select de status (Pago/Pendente/Cancelado), date pickers para período (de-até), botão "Limpar filtros". Usar dados de `useAccounts()` e `useCategories()` para popular selects
- [ ] 8.6 Criar `src/features/transactions/components/TransactionTable.tsx` — tabela paginada (Shadcn Table) com colunas: Data Competência (formatDate), Descrição, Categoria, Conta, Valor (formatCurrency com cor), Status (Badge). Transações canceladas: texto riscado (`line-through`) e badge cinza. Clique na linha navega para detalhe. Indicadores visuais: parcela "2/6", recorrente "🔁", transferência "↔️"
- [ ] 8.7 Criar componente de paginação: botões Anterior/Próxima, indicador "Página X de Y", select de itens por página (10, 20, 50)

### Formulário de Criação

- [ ] 8.8 Criar schemas Zod para cada tipo de transação: `simpleTransactionSchema`, `installmentSchema`, `recurrenceSchema`, `transferSchema`
- [ ] 8.9 Criar `src/features/transactions/components/TransactionForm.tsx` — modal com abas (Shadcn Tabs): Simples, Parcelada, Recorrente, Transferência. Cada aba renderiza os campos relevantes. Campo de valor centralizado com R$ grande (fiel ao mockup). Campos comuns: descrição, categoria (select), conta (select), data competência, data vencimento, toggle status (Pago/Pendente). Campos específicos: parcelas (input para Parcelada), conta destino (select para Transferência)
- [ ] 8.10 Criar `src/features/transactions/components/InstallmentPreview.tsx` — tabela de preview com colunas: Parcela nº, Data Competência, Data Vencimento, Valor. Calculada automaticamente a partir do número de parcelas, valor total e data inicial. Exibida antes de confirmar a criação

### Ações de Cancelamento e Ajuste

- [ ] 8.11 Criar `src/features/transactions/components/CancelModal.tsx` — modal de confirmação com campo de motivo (textarea opcional), aviso sobre irreversibilidade, botões Cancelar/Confirmar
- [ ] 8.12 Criar `src/features/transactions/components/AdjustModal.tsx` — modal com campo de novo valor (numérico formatado) e justificativa (textarea obrigatória), exibição do valor atual vs novo valor

### Detalhe da Transação

- [ ] 8.13 Criar `src/features/transactions/components/TransactionDetail.tsx` — painel/página com: dados completos (conta, categoria, tipo, valor, datas, status), metadados (se é ajuste → link para original, se é parcela → "Parcela X de Y", se é recorrente → indicador, se é transferência → conta origem/destino), badges de status, botões de ação (Cancelar, Ajustar — desabilitados se já cancelada)
- [ ] 8.14 Criar componente de histórico de auditoria: timeline vertical com entries (data, ação, usuário, detalhes). Dados de `GET /api/v1/transactions/{id}/history`
- [ ] 8.15 Criar `src/features/transactions/pages/TransactionDetailPage.tsx` — página com TransactionDetail + timeline de auditoria; rota `/transactions/:id`

### Página Principal

- [ ] 8.16 Criar `src/features/transactions/pages/TransactionsPage.tsx` — composição: header com título + botão "Nova Transação", TransactionFilters, TransactionTable com paginação; modal de criação (TransactionForm); rota `/transactions`
- [ ] 8.17 Criar `src/features/transactions/index.ts` — barrel export
- [ ] 8.18 Atualizar rotas: `/transactions` → TransactionsPage, `/transactions/:id` → TransactionDetailPage

### Testes

- [ ] 8.19 Criar MSW handlers: mock de todos os endpoints de transações (GET, POST, PATCH)
- [ ] 8.20 Testes unitários: TransactionForm (abas, validação por tipo, submit), TransactionFilters (aplicar/limpar filtros), TransactionTable (renderização, cores, badges, paginação), CancelModal (motivo, confirmação), InstallmentPreview (cálculo de parcelas)
- [ ] 8.21 Testes de integração: fluxo criar transação simples → aparece na lista; filtrar por conta → lista filtrada; cancelar transação → badge atualizado; navegar para detalhe → dados exibidos

## Sequenciamento

- Bloqueado por: 3.0 (Auth), 4.0 (Backend — filtros e paginação), 6.0 (Contas — select de contas), 7.0 (Categorias — select de categorias)
- Desbloqueia: 10.0 (Polimento)
- Paralelizável: Não (depende de Contas e Categorias para selects nos formulários)

## Detalhes de Implementação

### Formulário — Referência Visual (mockup `nova-transacao/`)

```
┌──────────────────────────────────────────────────────────┐
│ Nova Transação                                    ✕     │
├──────────────────────────────────────────────────────────┤
│ [Simples] [Parcelada] [Recorrente] [Transferência]      │
│                                                          │
│              Valor da transação                          │
│              R$ ___0,00___                               │
│                                                          │
│ Descrição: ________________________________              │
│                                                          │
│ Categoria: [▼ Selecione]    Conta: [▼ Nubank]           │
│                                                          │
│ Dt. Competência: [📅]       Dt. Vencimento: [📅]        │
│                                                          │
│ ┌────────────────────────────────────────────────┐      │
│ │ ✅ Status do Pagamento                         │      │
│ │ Marque se a transação já foi realizada  [🔵]   │      │
│ └────────────────────────────────────────────────┘      │
│                                                          │
│ + Adicionar observações ou anexos                        │
├──────────────────────────────────────────────────────────┤
│                           [Cancelar] [💾 Salvar Transação]│
└──────────────────────────────────────────────────────────┘
```

### Sincronização de Filtros com URL

```typescript
function useTransactionFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters = useMemo(() => ({
    accountId: searchParams.get('accountId') ?? undefined,
    categoryId: searchParams.get('categoryId') ?? undefined,
    type: searchParams.get('type') ? Number(searchParams.get('type')) : undefined,
    status: searchParams.get('status') ? Number(searchParams.get('status')) : undefined,
    dateFrom: searchParams.get('dateFrom') ?? undefined,
    dateTo: searchParams.get('dateTo') ?? undefined,
    page: Number(searchParams.get('page') ?? 1),
    size: Number(searchParams.get('size') ?? 20),
  }), [searchParams]);

  const setFilter = (key: string, value: string | undefined) => {
    setSearchParams(prev => {
      if (value) prev.set(key, value);
      else prev.delete(key);
      prev.set('page', '1'); // reset page on filter change
      return prev;
    });
  };

  const clearFilters = () => setSearchParams({});

  return { filters, setFilter, clearFilters };
}
```

### Preview de Parcelas — Cálculo

```typescript
function calculateInstallments(
  totalAmount: number,
  count: number,
  firstDate: Date
): InstallmentPreview[] {
  const installmentValue = Math.floor((totalAmount * 100) / count) / 100;
  const remainder = totalAmount - installmentValue * count;

  return Array.from({ length: count }, (_, i) => ({
    number: i + 1,
    competenceDate: addMonths(firstDate, i),
    amount: i === 0 ? installmentValue + remainder : installmentValue,
  }));
}
```

### Badges de Status

| Status | Badge classes | Texto |
|--------|-------------|-------|
| Pago | `bg-green-100 text-green-800` | Pago |
| Pendente | `bg-yellow-100 text-yellow-800` | Pendente |
| Cancelado | `bg-gray-100 text-gray-800 line-through` | Cancelado |

### Cores de Valor

| Tipo | Classe | Exemplo |
|------|--------|---------|
| Credit | `text-success` | R$ 1.500,00 (verde) |
| Debit | `text-danger` | - R$ 350,00 (vermelho) |

## Critérios de Sucesso

- Listagem exibe transações com paginação funcional (anterior/próxima, X de Y páginas)
- Filtros aplicam corretamente e são sincronizados com a URL (compartilháveis)
- "Limpar filtros" reseta todos os filtros e volta à página 1
- Formulário com 4 abas funciona corretamente para cada tipo de transação
- Validação inline por tipo: campos obrigatórios, valor > 0, datas válidas
- Preview de parcelas calcula corretamente valores e datas
- Cancelamento: modal com motivo, confirmação, toast de sucesso, status atualizado na lista
- Ajuste: modal com novo valor e justificativa, toast de sucesso
- Detalhe da transação exibe todos os metadados (parcela, recorrente, ajuste, transferência)
- Histórico de auditoria exibe timeline com ações
- Valores formatados em R$ com cores corretas (verde/vermelho)
- Transações canceladas visualmente diferenciadas (riscado + badge cinza)
- Layout fiel aos mockups `screen-examples/nova-transacao/` e `screen-examples/historico-financeiro/`
- Todos os testes passam
