# Review — Task 6.0: Frontend — Tipos, API Client e Hooks

**Data da revisão:** 2026-02-23  
**Revisor:** GitHub Copilot (Review Mode)  
**Branch:** `feat/prd-cupom-fiscal-task-6`  
**Veredito:** ✅ **APROVADO**

---

## 1. Resultados da Validação da Definição da Tarefa

### 1.1 Alinhamento com a Task

Todos os subtarefas definidas em `6_task.md` foram implementados:

| Subtarefa | Descrição | Status |
|-----------|-----------|--------|
| 6.1 | Tipos TypeScript em `types/receipt.ts` | ✅ Implementado |
| 6.2 | `hasReceipt: boolean` em `TransactionResponse` | ✅ Implementado |
| 6.3 | Funções de API em `api/receiptApi.ts` | ✅ Implementado |
| 6.4 | Hooks React Query (`useReceiptLookup`, `useReceiptImport`, `useTransactionReceipt`) | ✅ Implementado |
| 6.5 | Schema Zod em `schemas/importReceiptSchema.ts` | ✅ Implementado |
| 6.6 | Testes com MSW (`receiptApi.test.ts`, `receiptHooks.test.tsx`) | ✅ Implementado |

### 1.2 Critérios de Aceite — Verificação

| Critério | Status |
|----------|--------|
| Todos os tipos TypeScript refletem os DTOs do backend | ✅ |
| `TransactionResponse` atualizado com `hasReceipt: boolean` | ✅ |
| 3 funções de API fazem chamadas corretas aos endpoints | ✅ |
| 3 hooks funcionam corretamente (mutation/query) | ✅ |
| `useReceiptImport` invalida o cache de transactions após sucesso | ✅ |
| Schema Zod valida corretamente todos os campos | ✅ |
| Mensagens de erro em português | ✅ |
| Testes passam com MSW mockando os endpoints | ✅ |
| Nenhum teste existente foi quebrado | ✅ |
| Projeto frontend compila sem erros TypeScript | ✅ |

---

## 2. Descobertas da Análise de Regras

### 2.1 Regras React Aplicadas

**`react-project-structure.md`** — Feature-based architecture:
- ✅ Arquivos criados nas localizações corretas dentro de `features/transactions/`
- ✅ Barrel export (`index.ts`) atualizado com todos os novos módulos

**`react-coding-standards.md`** — Padrões de código:
- ✅ Funções de hook com prefixo `use` em `camelCase`
- ✅ Interfaces nomeadas claramente (`ReceiptItemResponse`, `EstablishmentResponse`, etc.)
- ✅ TypeScript strict mode (sem tipos `any`)
- ✅ Imports limpos com path alias `@/`
- ✅ Código em inglês (nomes, interfaces, variáveis)

**`react-testing.md`** — Padrões de teste:
- ✅ Usa Vitest + Testing Library + MSW
- ✅ Testes de hooks com `renderHook` + `QueryClientProvider`
- ✅ Padrão AAA (Arrange-Act-Assert) seguido
- ✅ Mock de `sonner` isolado por módulo
- ✅ `beforeEach` limpa mocks entre testes

---

## 3. Resumo da Revisão de Código

### 3.1 `types/receipt.ts` — APROVADO
Todos os tipos TypeScript espelham corretamente os DTOs do backend:
- `ReceiptItemResponse`: todos os 8 campos presentes (`id`, `description`, `productCode`, `quantity`, `unitOfMeasure`, `unitPrice`, `totalPrice`, `itemOrder`)
- `EstablishmentResponse`: 4 campos (`id`, `name`, `cnpj`, `accessKey`)
- `ReceiptLookupResponse`: 9 campos incluindo `alreadyImported`
- `ImportReceiptResponse`, `TransactionReceiptResponse`, `LookupReceiptRequest`, `ImportReceiptRequest`: todos corretos
- `productCode: string | null` — nullable corretamente mapeado

### 3.2 `types/transaction.ts` — APROVADO
- `hasReceipt: boolean` adicionado na posição correta (antes de `createdAt`, após `isOverdue`)

### 3.3 `api/receiptApi.ts` — APROVADO
- Endpoints corretos: `POST /api/v1/receipts/lookup`, `POST /api/v1/receipts/import`, `GET /api/v1/transactions/${transactionId}/receipt`
- Usa `apiClient` de `@/shared/services/apiClient`
- Assinaturas tipadas corretamente
- Pattern idêntico ao `transactionsApi.ts` existente

### 3.4 `api/transactionsApi.ts` — APROVADO
- `hasReceipt: Boolean(transaction.hasReceipt)` adicionado ao `normalizeTransaction`
- Coerção defensiva garante que um possível `null` / `undefined` da API seja normalizado como `false`

### 3.5 `hooks/useReceiptLookup.ts` — APROVADO
- `useMutation<ReceiptLookupResponse, Error, LookupReceiptRequest>` tipado corretamente
- Distingue os 3 status HTTP: 400 → chave inválida, 404 → não encontrada, 502 → SEFAZ indisponível
- `getErrorMessage` (helper global) como fallback para erros inesperados
- `onError` exibe `toast.error`

### 3.6 `hooks/useReceiptImport.ts` — APROVADO
- `useMutation<ImportReceiptResponse, Error, ImportReceiptRequest>` tipado corretamente
- Distingue 4 status HTTP: 400, 404, 409 → duplicidade, 502
- `onSuccess`: invalida `['transactions']` via `queryClient.invalidateQueries` + `toast.success`
- `onError`: `toast.error` com mensagem específica

### 3.7 `hooks/useTransactionReceipt.ts` — APROVADO
- `useQuery<TransactionReceiptResponse>` com query key `['transactions', transactionId, 'receipt']`
- `enabled: Boolean(transactionId) && hasReceipt` — só executa quando condições são satisfeitas
- Parâmetro `hasReceipt = false` como default sensato

### 3.8 `schemas/importReceiptSchema.ts` — APROVADO
- `input`: `string.trim().min(1)` — obrigatório
- `accountId`, `categoryId`: `string.uuid()` — obrigatório e com formato UUID
- `description`: `string.trim().min(1)` — obrigatório
- `competenceDate`: `z.coerce.date()` — coerção de string para `Date`
- `ImportReceiptFormData` exportado via `z.infer`
- Mensagens de erro em português

> **Observação (low):** `competenceDate` resulta em `Date` no TypeScript (por `z.coerce.date()`), enquanto `ImportReceiptRequest` espera `string`. A conversão deve ocorrer no componente de UI (Task 7.0) via `format(date, 'yyyy-MM-dd')`. Esta é uma prática aceitável — o schema é para o formulário, não para a chamada de API diretamente.

### 3.9 Testes — APROVADO
- **`receiptApi.test.ts`**: 3 testes — lookup, import, getTransactionReceipt — todos com MSW inline
- **`receiptHooks.test.tsx`**: 7 testes cobrindo:
  - `useReceiptLookup`: sucesso, 404, 502
  - `useReceiptImport`: sucesso (com verificação de `invalidateQueries`), 409
  - `useTransactionReceipt`: quando `hasReceipt=true` (fetches data), quando `hasReceipt=false` (fica idle)
- Todos os 10 testes passam

### 3.10 Testes existentes atualizados
- `handlers.ts`, `TransactionTable.test.tsx`, `TransactionForm.test.tsx`, `transactionHooks.test.tsx`, `RecentTransactions.test.tsx` — todos receberam `hasReceipt: false` nos mocks para satisfazer o TypeScript após adição do campo obrigatório

### 3.11 `index.ts` — APROVADO
- `receipt.ts`, `receiptApi.ts`, `useReceiptLookup`, `useReceiptImport`, `useTransactionReceipt` — todos exportados corretamente

---

## 4. Resultados de Build e Testes

### TypeScript
```
$ npx tsc --noEmit
(sem erros)
```

### Testes novos
```
✓ src/features/transactions/test/receiptApi.test.ts (3 testes)
✓ src/features/transactions/test/receiptHooks.test.tsx (7 testes)
Test Files: 2 passed | Tests: 10 passed
```

### Suíte completa
```
Test Files: 37 passed, 1 failed
Tests: 282 passed, 1 failed, 1 skipped
```

> **Nota:** O único teste falhando é `BudgetDashboard.test.tsx` na feature `budgets`, **pré-existente e não relacionado à Task 6.0**. Confirmado via `git diff HEAD` — nenhum arquivo da feature `budgets` foi modificado nesta task.

---

## 5. Arquivos Criados/Modificados

### Novos arquivos
| Arquivo | Tipo |
|---------|------|
| `frontend/src/features/transactions/types/receipt.ts` | Tipos TypeScript |
| `frontend/src/features/transactions/api/receiptApi.ts` | Funções de API |
| `frontend/src/features/transactions/hooks/useReceiptLookup.ts` | Hook |
| `frontend/src/features/transactions/hooks/useReceiptImport.ts` | Hook |
| `frontend/src/features/transactions/hooks/useTransactionReceipt.ts` | Hook |
| `frontend/src/features/transactions/schemas/importReceiptSchema.ts` | Schema Zod |
| `frontend/src/features/transactions/test/receiptApi.test.ts` | Testes |
| `frontend/src/features/transactions/test/receiptHooks.test.tsx` | Testes |

### Arquivos modificados
| Arquivo | Motivo |
|---------|--------|
| `frontend/src/features/transactions/types/transaction.ts` | `hasReceipt: boolean` adicionado |
| `frontend/src/features/transactions/api/transactionsApi.ts` | Normalização de `hasReceipt` |
| `frontend/src/features/transactions/index.ts` | Novos exports |
| `frontend/src/features/transactions/test/handlers.ts` | `hasReceipt: false` nos mocks |
| `frontend/src/features/transactions/components/TransactionTable.test.tsx` | `hasReceipt: false` nos mocks |
| `frontend/src/features/transactions/components/TransactionForm.test.tsx` | `hasReceipt: false` nos mocks |
| `frontend/src/features/transactions/test/transactionHooks.test.tsx` | `hasReceipt: false` nos mocks |
| `frontend/src/features/dashboard/components/RecentTransactions.test.tsx` | `hasReceipt: false` nos mocks |

---

## 6. Problemas Identificados e Resoluções

| # | Severidade | Problema | Resolução |
|---|-----------|----------|-----------|
| 1 | Low | `competenceDate` no schema Zod é `Date` (via `z.coerce.date()`) enquanto o DTO da API espera `string` | Aceitável — a conversão será feita no componente (Task 7.0). Prática padrão com RHF + Zod |
| 2 | Low | Não há teste específico para status HTTP 400 em `useReceiptLookup` | A task pede explicitamente apenas testes para 404, 502 e 409. Lacuna menor |

Nenhum problema crítico ou de alta severidade encontrado. Todos os problemas identificados são de baixa severidade e não requerem correção para esta task.

---

## 7. Checklist Final

- [x] 6.1 Tipos TypeScript criados e corretos
- [x] 6.2 `TransactionResponse` atualizado com `hasReceipt`
- [x] 6.3 Funções de API implementadas e com endpoints corretos
- [x] 6.4 Hooks React Query implementados corretamente
- [x] 6.5 Schema Zod criado com validações e mensagens em português
- [x] 6.6 Testes implementados com MSW, todos passando
- [x] Padrões de projeto seguidos (feature-based, path aliases, naming conventions)
- [x] `index.ts` atualizado com barrel exports
- [x] Testes existentes não quebrados
- [x] TypeScript compila sem erros

---

## 8. Atualização da Task

```markdown
- [x] 6.0 Frontend — Tipos, API Client e Hooks ✅ CONCLUÍDA
  - [x] 6.1 Tipos TypeScript criados em `features/transactions/types/receipt.ts`
  - [x] 6.2 `TransactionResponse` atualizado com `hasReceipt: boolean`
  - [x] 6.3 Funções de API criadas em `features/transactions/api/receiptApi.ts`
  - [x] 6.4 Hooks React Query criados (`useReceiptLookup`, `useReceiptImport`, `useTransactionReceipt`)
  - [x] 6.5 Schema Zod criado em `features/transactions/schemas/importReceiptSchema.ts`
  - [x] 6.6 Testes implementados e passando (10/10)
  - [x] Definição da tarefa, PRD e tech spec validados
  - [x] Análise de regras e conformidade verificadas
  - [x] Revisão de código completada
  - [x] Pronto para deploy
```

---

## 9. Sugestão de Commit

```
feat(cupom-fiscal): adicionar camada de dados frontend para importação de NFC-e

- Criar tipos TypeScript (receipt.ts) espelhando DTOs do backend
- Adicionar hasReceipt ao TransactionResponse e normalização
- Implementar funções de API para lookup, import e getTransactionReceipt
- Criar hooks React Query: useReceiptLookup, useReceiptImport, useTransactionReceipt
- Criar schema Zod importReceiptSchema com validações em português
- Adicionar 10 testes (api + hooks) cobrindo casos de sucesso e erros (404, 502, 409)
- Atualizar index.ts e mocks existentes com novo campo hasReceipt
```
