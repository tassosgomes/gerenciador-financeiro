---
status: done
parallelizable: false
blocked_by: ["5.0"]
---

<task_context>
<domain>frontend</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"7.0"</unblocks>
</task_context>

# Tarefa 6.0: Frontend — Tipos, API Client e Hooks

## Visão Geral

Criar a camada de dados do frontend para o recurso de Importação de Cupom Fiscal. Inclui os tipos TypeScript que espelham os DTOs do backend, as funções de chamada à API via Axios, e os hooks React Query para consulta e mutação. Também atualiza o tipo `TransactionResponse` existente com o novo campo `hasReceipt`. Esta tarefa é a base para os componentes de UI da Task 7.0.

## Requisitos

- Criar tipos TypeScript que espelham os DTOs do backend (responses e requests)
- Criar funções de API para os 3 endpoints: lookup, import, getTransactionReceipt
- Criar hooks React Query: `useReceiptLookup` (mutation), `useReceiptImport` (mutation), `useTransactionReceipt` (query)
- Atualizar tipo `TransactionResponse` com campo `hasReceipt`
- Seguir padrões existentes do projeto (feature-based architecture, apiClient com Axios)
- Criar schema Zod para validação do formulário de importação

## Subtarefas

- [x] 6.1 Criar tipos TypeScript em `features/transactions/types/`
  - `ReceiptItemResponse` — id, description, productCode, quantity, unitOfMeasure, unitPrice, totalPrice, itemOrder
  - `EstablishmentResponse` — id, name, cnpj, accessKey
  - `ReceiptLookupResponse` — accessKey, establishmentName, establishmentCnpj, issuedAt, totalAmount, discountAmount, paidAmount, items (ReceiptItemResponse[]), alreadyImported
  - `ImportReceiptResponse` — transaction (TransactionResponse), establishment (EstablishmentResponse), items (ReceiptItemResponse[])
  - `TransactionReceiptResponse` — establishment (EstablishmentResponse), items (ReceiptItemResponse[])
  - `LookupReceiptRequest` — input (string)
  - `ImportReceiptRequest` — accessKey, accountId, categoryId, description, competenceDate, operationId?

- [x] 6.2 Atualizar tipo `TransactionResponse` existente
  - Adicionar campo `hasReceipt: boolean`
  - Localizar o tipo em `features/transactions/types/` e adicionar o campo

- [x] 6.3 Criar funções de API em `features/transactions/api/`
  - `lookupReceipt(request: LookupReceiptRequest): Promise<ReceiptLookupResponse>`
    - `POST /api/v1/receipts/lookup`
  - `importReceipt(request: ImportReceiptRequest): Promise<ImportReceiptResponse>`
    - `POST /api/v1/receipts/import`
  - `getTransactionReceipt(transactionId: string): Promise<TransactionReceiptResponse>`
    - `GET /api/v1/transactions/${transactionId}/receipt`

- [x] 6.4 Criar hooks React Query em `features/transactions/hooks/`
  - **`useReceiptLookup`** (mutation):
    - Usa `useMutation` com `lookupReceipt`
    - Retorna `mutate`, `data`, `isPending`, `error`
    - Toast de erro em caso de falha (distinguir: SEFAZ indisponível, NFC-e não encontrada, chave inválida)
  - **`useReceiptImport`** (mutation):
    - Usa `useMutation` com `importReceipt`
    - Em caso de sucesso: invalida query de transactions, toast de sucesso
    - Em caso de erro: toast com mensagem (duplicidade, SEFAZ indisponível, etc.)
  - **`useTransactionReceipt`** (query):
    - Usa `useQuery` com `getTransactionReceipt`
    - Query key: `['transactions', transactionId, 'receipt']`
    - Enabled: só quando `transactionId` é válido e `hasReceipt` é true

- [x] 6.5 Criar schema Zod para formulário de importação em `features/transactions/schemas/`
  - `importReceiptSchema`:
    - `input`: string, obrigatório, mínimo 1 caractere
    - `accountId`: string (uuid), obrigatório
    - `categoryId`: string (uuid), obrigatório
    - `description`: string, obrigatório
    - `competenceDate`: date, obrigatório
  - Mensagens de erro em português (padrão do projeto)

- [x] 6.6 Testes dos hooks e funções de API
  - Mock dos endpoints via MSW
  - Testar `useReceiptLookup`: chamada bem-sucedida retorna dados
  - Testar `useReceiptImport`: chamada bem-sucedida invalida cache de transactions
  - Testar `useTransactionReceipt`: retorna dados quando transação possui cupom
  - Testar tratamento de erros (409, 502, 404)

## Sequenciamento

- Bloqueado por: 5.0 (API endpoints devem estar definidos para criar os tipos e chamadas)
- Desbloqueia: 7.0 (Frontend — Página e UI)
- Paralelizável: Não (depende da API pronta)

## Detalhes de Implementação

### Localização dos Arquivos

| Arquivo | Caminho |
|---------|---------|
| Tipos Receipt | `frontend/src/features/transactions/types/receipt.ts` |
| Tipo TransactionResponse | `frontend/src/features/transactions/types/` (arquivo existente) |
| Funções API | `frontend/src/features/transactions/api/receiptApi.ts` |
| Hooks | `frontend/src/features/transactions/hooks/useReceiptLookup.ts` |
|        | `frontend/src/features/transactions/hooks/useReceiptImport.ts` |
|        | `frontend/src/features/transactions/hooks/useTransactionReceipt.ts` |
| Schema | `frontend/src/features/transactions/schemas/importReceiptSchema.ts` |
| Testes | `frontend/src/features/transactions/test/` |
| MSW Mock Handlers | `frontend/src/shared/test/mocks/handlers/` (ou inline nos testes) |

### Padrões a Seguir

- Consultar tipos existentes em `features/transactions/types/` para convenções
- Consultar funções de API existentes em `features/transactions/api/` para padrão de uso do `apiClient`
- Consultar hooks existentes em `features/transactions/hooks/` para padrão React Query
- Consultar schemas existentes em `features/transactions/schemas/` para padrão Zod
- Usar `apiClient` de `shared/services/apiClient.ts` para chamadas HTTP

### Exemplo de Função de API

```typescript
import { apiClient } from '@/shared/services/apiClient';
import type { LookupReceiptRequest, ReceiptLookupResponse } from '../types/receipt';

export async function lookupReceipt(request: LookupReceiptRequest): Promise<ReceiptLookupResponse> {
  const response = await apiClient.post<ReceiptLookupResponse>('/api/v1/receipts/lookup', request);
  return response.data;
}
```

### Exemplo de Hook (Mutation)

```typescript
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { lookupReceipt } from '../api/receiptApi';
import type { LookupReceiptRequest, ReceiptLookupResponse } from '../types/receipt';

export function useReceiptLookup() {
  return useMutation<ReceiptLookupResponse, Error, LookupReceiptRequest>({
    mutationFn: lookupReceipt,
    onError: (error) => {
      // Distinguir tipo de erro pelo status HTTP
      toast.error('Erro ao consultar cupom fiscal');
    },
  });
}
```

### Tratamento de Erros no Frontend

| HTTP Status | Mensagem ao Usuário |
|-------------|---------------------|
| 400 | "Chave de acesso inválida. Informe os 44 dígitos numéricos." |
| 404 | "NFC-e não encontrada. Verifique se a nota está disponível na SEFAZ." |
| 409 | "Este cupom fiscal já foi importado anteriormente." |
| 502 | "A SEFAZ está indisponível no momento. Tente novamente mais tarde." |

## Critérios de Sucesso

- Todos os tipos TypeScript refletem corretamente os DTOs do backend
- `TransactionResponse` atualizado com `hasReceipt: boolean`
- As 3 funções de API fazem chamadas corretas aos endpoints
- Os 3 hooks funcionam corretamente (mutation para lookup/import, query para receipt)
- `useReceiptImport` invalida o cache de transactions após sucesso
- Schema Zod valida corretamente todos os campos do formulário de importação
- Mensagens de erro em português
- Testes passam com MSW mockando os endpoints
- Nenhum teste existente foi quebrado
- Projeto frontend compila sem erros TypeScript
