---
status: done
parallelizable: false
blocked_by: []
---

<task_context>
<domain>backend/domain</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>none</dependencies>
<unblocks>"2.0", "3.0"</unblocks>
</task_context>

# Tarefa 1.0: Entidades de Domínio, DTOs e Interfaces

## Visão Geral

Criar toda a fundação da camada de domínio para o recurso de Importação de Cupom Fiscal (NFC-e). Isso inclui as novas entidades `ReceiptItem` e `Establishment`, os DTOs de domínio `NfceData` e `NfceItemData`, as interfaces de repositório e serviço, e as domain exceptions específicas. Esta tarefa é a base para todas as demais — nenhuma outra tarefa pode começar antes desta ser concluída.

## Requisitos

- Criar entidade `ReceiptItem` com factory method `Create()` seguindo o padrão das entidades existentes (`BaseEntity`)
- Criar entidade `Establishment` com factory method `Create()` seguindo o padrão das entidades existentes (`BaseEntity`)
- Criar DTOs de domínio `NfceData` e `NfceItemData` como records imutáveis
- Criar interface `ISefazNfceService` com método `LookupAsync`
- Criar interfaces `IReceiptItemRepository` e `IEstablishmentRepository` seguindo o padrão de repositório do projeto
- Criar 5 domain exceptions fortemente tipadas

## Subtarefas

- [x] 1.1 Criar entidade `ReceiptItem` em `Domain/Entity/ReceiptItem.cs`
  - Herdar de `BaseEntity`
  - Propriedades: `TransactionId` (Guid), `Description` (string), `ProductCode` (string?), `Quantity` (decimal), `UnitOfMeasure` (string), `UnitPrice` (decimal), `TotalPrice` (decimal), `ItemOrder` (int)
  - Navigation property: `Transaction`
  - Factory method estático `Create(...)` que chama `SetAuditOnCreate(userId)`
  - Private setters em todas as propriedades

- [x] 1.2 Criar entidade `Establishment` em `Domain/Entity/Establishment.cs`
  - Herdar de `BaseEntity`
  - Propriedades: `TransactionId` (Guid), `Name` (string), `Cnpj` (string), `AccessKey` (string)
  - Navigation property: `Transaction`
  - Factory method estático `Create(...)` que chama `SetAuditOnCreate(userId)`
  - Private setters em todas as propriedades

- [x] 1.3 Criar DTOs de domínio em `Domain/Dto/NfceData.cs`
  - `NfceData` — record com: `AccessKey`, `EstablishmentName`, `EstablishmentCnpj`, `IssuedAt` (DateTime), `TotalAmount`, `DiscountAmount`, `PaidAmount`, `Items` (IReadOnlyList<NfceItemData>)
  - `NfceItemData` — record com: `Description`, `ProductCode` (string?), `Quantity`, `UnitOfMeasure`, `UnitPrice`, `TotalPrice`

- [x] 1.4 Criar interface `ISefazNfceService` em `Domain/Interface/ISefazNfceService.cs`
  - Método: `Task<NfceData> LookupAsync(string accessKey, CancellationToken cancellationToken)`

- [x] 1.5 Criar interface `IReceiptItemRepository` em `Domain/Interface/IReceiptItemRepository.cs`
  - Métodos: `AddRangeAsync(IEnumerable<ReceiptItem>, CancellationToken)`, `GetByTransactionIdAsync(Guid, CancellationToken)`, `RemoveRange(IEnumerable<ReceiptItem>)`

- [x] 1.6 Criar interface `IEstablishmentRepository` em `Domain/Interface/IEstablishmentRepository.cs`
  - Métodos: `AddAsync(Establishment, CancellationToken)`, `GetByTransactionIdAsync(Guid, CancellationToken)`, `Remove(Establishment)`, `ExistsByAccessKeyAsync(string, CancellationToken)`

- [x] 1.7 Criar domain exceptions em `Domain/Exception/`
  - `InvalidAccessKeyException` — chave de acesso com formato inválido (não são 44 dígitos numéricos)
  - `NfceNotFoundException` — NFC-e não encontrada na SEFAZ (nota expirada, inválida)
  - `SefazUnavailableException` — SEFAZ indisponível (timeout, erro de conexão, 5xx)
  - `SefazParsingException` — erro ao parsear o HTML retornado pela SEFAZ
  - `DuplicateReceiptException` — tentativa de importar cupom com chave de acesso já existente
  - Todas devem herdar de `DomainException` (padrão existente do projeto)

- [x] 1.8 Testes unitários das entidades e validações
  - Testar `ReceiptItem.Create()` com todos os campos preenchidos
  - Testar `Establishment.Create()` com todos os campos preenchidos
  - Verificar que `SetAuditOnCreate` é chamado corretamente (CreatedBy, CreatedAt)

## Sequenciamento

- Bloqueado por: Nenhum
- Desbloqueia: 2.0 (Infraestrutura), 3.0 (Serviço SEFAZ)
- Paralelizável: Não (é a primeira tarefa, foundation)

## Detalhes de Implementação

### Localização dos Arquivos

| Arquivo | Caminho |
|---------|---------|
| `ReceiptItem.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Entity/` |
| `Establishment.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Entity/` |
| `NfceData.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Dto/` |
| `ISefazNfceService.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Interface/` |
| `IReceiptItemRepository.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Interface/` |
| `IEstablishmentRepository.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Interface/` |
| `InvalidAccessKeyException.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Exception/` |
| `NfceNotFoundException.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Exception/` |
| `SefazUnavailableException.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Exception/` |
| `SefazParsingException.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Exception/` |
| `DuplicateReceiptException.cs` | `backend/3-Domain/GestorFinanceiro.Financeiro.Domain/Exception/` |

### Padrões a Seguir

- Consultar `BaseEntity` existente para entender campos de auditoria (Id, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
- Consultar `DomainException` existente para padrão de herança de exceções
- Consultar entidades existentes como `Transaction`, `Account`, `Category` para consistência de estilo
- Consultar repositórios existentes como `ITransactionRepository`, `IAccountRepository` para padrão de interface

### Código de Referência (Entidade ReceiptItem)

```csharp
public class ReceiptItem : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ProductCode { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public int ItemOrder { get; private set; }

    public Transaction Transaction { get; private set; } = null!;

    public static ReceiptItem Create(
        Guid transactionId, string description, string? productCode,
        decimal quantity, string unitOfMeasure, decimal unitPrice,
        decimal totalPrice, int itemOrder, string userId)
    {
        var item = new ReceiptItem
        {
            TransactionId = transactionId,
            Description = description,
            ProductCode = productCode,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            ItemOrder = itemOrder,
        };
        item.SetAuditOnCreate(userId);
        return item;
    }
}
```

### Código de Referência (Entidade Establishment)

```csharp
public class Establishment : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Cnpj { get; private set; } = string.Empty;
    public string AccessKey { get; private set; } = string.Empty;

    public Transaction Transaction { get; private set; } = null!;

    public static Establishment Create(
        Guid transactionId, string name, string cnpj,
        string accessKey, string userId)
    {
        var establishment = new Establishment
        {
            TransactionId = transactionId,
            Name = name,
            Cnpj = cnpj,
            AccessKey = accessKey,
        };
        establishment.SetAuditOnCreate(userId);
        return establishment;
    }
}
```

## Critérios de Sucesso

- Todas as entidades compilam sem erros e seguem o padrão de `BaseEntity`
- DTOs `NfceData` e `NfceItemData` são records imutáveis com todos os campos especificados
- Todas as 3 interfaces de repositório/serviço estão criadas com assinaturas corretas
- Todas as 5 exceptions herdam de `DomainException` e possuem mensagens descritivas
- Testes unitários passam para `ReceiptItem.Create()` e `Establishment.Create()`
- O projeto `GestorFinanceiro.Financeiro.Domain` compila sem erros
- Nenhum teste existente foi quebrado
