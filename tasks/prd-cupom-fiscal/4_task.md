---
status: pending
parallelizable: false
blocked_by: ["1.0", "2.0", "3.0"]
---

<task_context>
<domain>backend/application</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database</dependencies>
<unblocks>"5.0"</unblocks>
</task_context>

# Tarefa 4.0: Commands, Queries e Handlers (Application Layer)

## Visão Geral

Implementar toda a lógica de aplicação para o recurso de Importação de Cupom Fiscal. Esta é a tarefa mais complexa do backend, pois orquestra a consulta da SEFAZ, a criação atômica de transação + estabelecimento + itens, a detecção de duplicidade, e a extensão do handler de cancelamento de transação. Também inclui os DTOs de resposta, validators do FluentValidation, mappings do Mapster, e a adição do campo `hasReceipt` ao `TransactionResponse` existente.

## Requisitos

- Implementar `LookupReceiptCommand` + Handler + Validator (consulta SEFAZ e retorna preview)
- Implementar `ImportReceiptCommand` + Handler + Validator (cria transação + estabelecimento + itens atomicamente)
- Implementar `GetTransactionReceiptQuery` + Handler (retorna itens e estabelecimento de uma transação)
- Criar DTOs de resposta: `ReceiptLookupResponse`, `ImportReceiptResponse`, `ReceiptItemResponse`, `EstablishmentResponse`
- Adicionar campo `HasReceipt` ao `TransactionResponse` existente
- Estender `CancelTransactionCommandHandler` para remover itens e estabelecimento ao cancelar transação com cupom
- Registrar handlers no DI via `ApplicationServiceExtensions`
- Adicionar mappings no `MappingConfig` (Mapster)
- Testes unitários de todos os handlers e validators

## Subtarefas

- [ ] 4.1 Criar DTOs de resposta em `Application/Dtos/`
  - `ReceiptItemResponse` — Id, Description, ProductCode, Quantity, UnitOfMeasure, UnitPrice, TotalPrice, ItemOrder
  - `EstablishmentResponse` — Id, Name, Cnpj, AccessKey
  - `ReceiptLookupResponse` — AccessKey, EstablishmentName, EstablishmentCnpj, IssuedAt, TotalAmount, DiscountAmount, PaidAmount, Items (list), AlreadyImported (bool)
  - `ImportReceiptResponse` — Transaction (TransactionResponse), Establishment (EstablishmentResponse), Items (list of ReceiptItemResponse)
  - `TransactionReceiptResponse` — Establishment (EstablishmentResponse), Items (list of ReceiptItemResponse)

- [ ] 4.2 Adicionar `HasReceipt` ao `TransactionResponse` existente
  - Tipo: `bool`
  - Valor computado: `true` se existe registro em `Establishment` para o `TransactionId`
  - Atualizar o `ListTransactionsQueryHandler` para popular o campo via left join ou subquery

- [ ] 4.3 Criar `LookupReceiptCommand` + Handler + Validator
  - **Command**: `LookupReceiptCommand : ICommand<ReceiptLookupResponse>` com campo `Input` (string — chave ou URL)
  - **Validator** (`LookupReceiptValidator`):
    - `Input` não pode ser vazio
    - Se parece chave (só dígitos): deve ter exatamente 44 dígitos
    - Se parece URL: deve conter `http`
  - **Handler** (`LookupReceiptCommandHandler`):
    1. Extrair chave de acesso do `Input` (se URL, extrair da URL)
    2. Chamar `ISefazNfceService.LookupAsync(accessKey, ct)`
    3. Verificar se a chave já foi importada via `IEstablishmentRepository.ExistsByAccessKeyAsync`
    4. Montar e retornar `ReceiptLookupResponse` com `AlreadyImported = true/false`

- [ ] 4.4 Criar `ImportReceiptCommand` + Handler + Validator
  - **Command**: `ImportReceiptCommand : ICommand<ImportReceiptResponse>` com campos:
    - `AccessKey` (string), `AccountId` (Guid), `CategoryId` (Guid), `Description` (string), `CompetenceDate` (DateOnly), `OperationId` (string?)
  - **Validator** (`ImportReceiptValidator`):
    - `AccessKey`: obrigatório, 44 dígitos numéricos
    - `AccountId`: obrigatório, Guid válido
    - `CategoryId`: obrigatório, Guid válido
    - `Description`: obrigatório, não vazio
    - `CompetenceDate`: obrigatório, não futuro
  - **Handler** (`ImportReceiptCommandHandler`):
    1. Verificar duplicidade via `IEstablishmentRepository.ExistsByAccessKeyAsync`. Se já importado, lançar `DuplicateReceiptException`
    2. Consultar SEFAZ via `ISefazNfceService.LookupAsync`
    3. Verificar se `Account` e `Category` existem
    4. Criar descrição: se houver desconto (`DiscountAmount > 0`), incluir nota: `"Desconto de R$ X,XX aplicado. Valor original: R$ Y,YY"`
    5. Criar `Transaction` de débito com status Paga, valor = `PaidAmount`, na conta e categoria selecionadas, data = `CompetenceDate`
    6. Criar `Establishment` vinculado à transação
    7. Criar `ReceiptItem`s vinculados à transação (um para cada item do cupom, com `ItemOrder` sequencial)
    8. Atualizar saldo da conta (regras existentes do sistema)
    9. Salvar tudo atomicamente via `IUnitOfWork.SaveChangesAsync`
    10. Registrar auditoria via `AuditService`
    11. Retornar `ImportReceiptResponse`

- [ ] 4.5 Criar `GetTransactionReceiptQuery` + Handler
  - **Query**: `GetTransactionReceiptQuery : IQuery<TransactionReceiptResponse>` com campo `TransactionId` (Guid)
  - **Handler** (`GetTransactionReceiptQueryHandler`):
    1. Buscar `Establishment` por `TransactionId`
    2. Se não encontrado, lançar exceção (transação não possui cupom)
    3. Buscar `ReceiptItem`s por `TransactionId`
    4. Montar e retornar `TransactionReceiptResponse`

- [ ] 4.6 Estender `CancelTransactionCommandHandler`
  - Injetar `IReceiptItemRepository` e `IEstablishmentRepository` no construtor
  - Após o cancelamento da transação (lógica existente), verificar se tem cupom:
    1. Buscar `ReceiptItem`s por `TransactionId`
    2. Buscar `Establishment` por `TransactionId`
    3. Se existirem, remover via repositórios (`RemoveRange`, `Remove`)
    4. Registrar auditoria de cascade delete

- [ ] 4.7 Adicionar mappings no `MappingConfig`
  - `ReceiptItem` → `ReceiptItemResponse`
  - `Establishment` → `EstablishmentResponse`
  - `NfceData` → `ReceiptLookupResponse` (incluindo mapeamento de `NfceItemData` para os items do response)

- [ ] 4.8 Registrar handlers no DI (`ApplicationServiceExtensions`)
  - `LookupReceiptCommandHandler`
  - `ImportReceiptCommandHandler`
  - `GetTransactionReceiptQueryHandler`

- [ ] 4.9 Testes unitários dos handlers
  - **LookupReceiptCommandHandler:**
    - Teste lookup bem-sucedido (mock SEFAZ retorna NfceData)
    - Teste com cupom já importado (AlreadyImported = true)
    - Teste com cupom não importado (AlreadyImported = false)
    - Teste com URL como input (extrai chave e consulta)
    - Teste quando SEFAZ lança exceção (propaga corretamente)
  - **ImportReceiptCommandHandler:**
    - Teste importação bem-sucedida (verifica transação, estabelecimento e itens criados)
    - Teste com desconto (verifica valor = PaidAmount e descrição com nota de desconto)
    - Teste sem desconto (verifica valor = PaidAmount = TotalAmount)
    - Teste duplicidade (chave já existe → DuplicateReceiptException)
    - Teste conta inexistente (→ exceção)
    - Teste categoria inexistente (→ exceção)
    - Teste que UnitOfWork.SaveChangesAsync é chamado (atomicidade)
    - Teste que AuditService é chamado
  - **GetTransactionReceiptQueryHandler:**
    - Teste retorno bem-sucedido com itens e estabelecimento
    - Teste transação sem cupom (→ exceção)
  - **CancelTransactionCommandHandler (extensão):**
    - Teste cancelamento de transação com cupom (remove itens e estabelecimento)
    - Teste cancelamento de transação sem cupom (comportamento existente mantido)

- [ ] 4.10 Testes unitários dos validators
  - **LookupReceiptValidator:**
    - Input vazio → erro
    - Chave válida (44 dígitos) → sem erro
    - Chave inválida (< 44, > 44, letras) → erro
    - URL válida → sem erro
  - **ImportReceiptValidator:**
    - Todos campos válidos → sem erro
    - AccessKey vazio/inválido → erro
    - AccountId vazio → erro
    - CategoryId vazio → erro
    - Description vazia → erro

## Sequenciamento

- Bloqueado por: 1.0, 2.0, 3.0 (precisa de entidades, repositórios e serviço SEFAZ)
- Desbloqueia: 5.0 (API Controller)
- Paralelizável: Não (depende de todas as tasks anteriores do backend)

## Detalhes de Implementação

### Localização dos Arquivos

| Arquivo | Caminho |
|---------|---------|
| DTOs | `backend/2-Application/GestorFinanceiro.Financeiro.Application/Dtos/` |
| LookupReceiptCommand + Handler + Validator | `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/Receipt/` |
| ImportReceiptCommand + Handler + Validator | `backend/2-Application/GestorFinanceiro.Financeiro.Application/Commands/Receipt/` |
| GetTransactionReceiptQuery + Handler | `backend/2-Application/GestorFinanceiro.Financeiro.Application/Queries/Receipt/` |
| MappingConfig | `backend/2-Application/GestorFinanceiro.Financeiro.Application/Common/` (ou local existente) |
| ApplicationServiceExtensions | `backend/2-Application/GestorFinanceiro.Financeiro.Application/Common/ApplicationServiceExtensions.cs` |
| Testes | `backend/5-Tests/GestorFinanceiro.Financeiro.UnitTests/` |

### Padrões a Seguir

- Consultar commands existentes (ex: `CreateTransactionCommand`, `CancelTransactionCommand`) para padrão CQRS
- Consultar validators existentes para padrão FluentValidation
- Consultar `TransactionDomainService` para regras de criação de transação
- Consultar `MappingConfig` existente para padrão de mapeamento Mapster
- Usar `IUnitOfWork` para atomicidade (padrão existente)
- Usar `AuditService` para log de auditoria (padrão existente)

### Importação com Desconto (Lógica de Negócio)

```
Se NfceData.DiscountAmount > 0:
  → Valor da transação = NfceData.PaidAmount
  → Descrição final = "{Description} — Desconto de R$ {DiscountAmount:N2} aplicado. Valor original: R$ {TotalAmount:N2}"
Senão:
  → Valor da transação = NfceData.PaidAmount (= TotalAmount)
  → Descrição final = "{Description}"
```

### Computação do `HasReceipt`

O campo `HasReceipt` no `TransactionResponse` deve ser computado no `ListTransactionsQueryHandler` via left join com `Establishment`:

```csharp
HasReceipt = await _context.Establishments
    .AnyAsync(e => e.TransactionId == transaction.Id, ct)
```

Ou preferencialmente via projeção no query para evitar N+1:
```csharp
.Select(t => new TransactionResponse
{
    // ... campos existentes
    HasReceipt = _context.Establishments.Any(e => e.TransactionId == t.Id)
})
```

## Critérios de Sucesso

- Todos os 3 handlers (Lookup, Import, GetReceipt) funcionam corretamente
- `ImportReceiptCommandHandler` cria transação + estabelecimento + itens atomicamente via UnitOfWork
- Detecção de duplicidade rejeita chave de acesso já importada com `DuplicateReceiptException`
- Importação com desconto usa `PaidAmount` como valor da transação e inclui nota na descrição
- `CancelTransactionCommandHandler` estendido remove itens e estabelecimento ao cancelar transação com cupom
- `HasReceipt` computado corretamente no `TransactionResponse`
- Validators validam corretamente todos os campos obrigatórios e formatos
- Mappings Mapster configurados para todas as novas entidades → DTOs
- Handlers registrados no DI
- Mínimo 20 testes unitários passando (handlers + validators)
- Todos os testes existentes continuam passando
