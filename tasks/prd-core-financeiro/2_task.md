---
status: pending
parallelizable: false
blocked_by: ["1.0"]
---

<task_context>
<domain>engine/domínio</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>low</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"3.0"</unblocks>
</task_context>

# Tarefa 2.0: Domain Layer — Enums e Base Entity

## Visão Geral

Implementar os tipos básicos do domínio: enums que classificam contas, categorias, transações e status, e a classe abstrata `BaseEntity` com campos de auditoria. Estes tipos são utilizados por todas as entidades e serviços do sistema.

## Requisitos

- PRD F9 req 40: toda entidade deve registrar `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`
- PRD F1 req 2: tipos de conta — `Corrente`, `Cartão`, `Investimento`, `Carteira`
- PRD F2 req 8: tipos de categoria — `Receita`, `Despesa`
- PRD F3 req 13: tipos de transação — `Debit`, `Credit`
- PRD F3 req 14: status de transação — `Paid`, `Pending`, `Cancelled` (Overdue é calculado on-the-fly)

## Subtarefas

- [ ] 2.1 Criar enum `AccountType` com valores: `Corrente = 1`, `Cartao = 2`, `Investimento = 3`, `Carteira = 4`
- [ ] 2.2 Criar enum `CategoryType` com valores: `Receita = 1`, `Despesa = 2`
- [ ] 2.3 Criar enum `TransactionType` com valores: `Debit = 1`, `Credit = 2`
- [ ] 2.4 Criar enum `TransactionStatus` com valores: `Paid = 1`, `Pending = 2`, `Cancelled = 3`
- [ ] 2.5 Criar classe abstrata `BaseEntity` com campos de auditoria (`Id`, `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`) e métodos `SetAuditOnCreate` e `SetAuditOnUpdate`
- [ ] 2.6 Testes unitários para `BaseEntity` (validar SetAuditOnCreate e SetAuditOnUpdate)

## Sequenciamento

- Bloqueado por: 1.0 (estrutura de solução deve existir)
- Desbloqueia: 3.0 (entidades usam os enums e BaseEntity)
- Paralelizável: Não (depende de 1.0, e 3.0 depende desta)

## Detalhes de Implementação

### Localização dos arquivos

```
3-Domain/GestorFinanceiro.Financeiro.Domain/
├── Enum/
│   ├── AccountType.cs
│   ├── CategoryType.cs
│   ├── TransactionType.cs
│   └── TransactionStatus.cs
└── Entity/
    └── BaseEntity.cs
```

### BaseEntity (conforme techspec)

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string CreatedBy { get; protected set; } = string.Empty;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    public void SetAuditOnCreate(string userId)
    {
        CreatedBy = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetAuditOnUpdate(string userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Enums

```csharp
public enum AccountType       { Corrente = 1, Cartao = 2, Investimento = 3, Carteira = 4 }
public enum CategoryType      { Receita = 1, Despesa = 2 }
public enum TransactionType   { Debit = 1, Credit = 2 }
public enum TransactionStatus { Paid = 1, Pending = 2, Cancelled = 3 }
```

### Observações

- Todos os timestamps devem estar em UTC
- Enums usam valores inteiros explícitos (para mapeamento no banco de dados)
- `Id` é `Guid` gerado client-side — sem dependência de auto-increment do banco
- `BaseEntity` é abstrata — não pode ser instanciada diretamente

## Critérios de Sucesso

- Todos os 4 enums compilam e possuem os valores definidos na techspec
- `BaseEntity` possui todos os campos de auditoria (F9 req 40)
- `SetAuditOnCreate` define `CreatedBy` e `CreatedAt` em UTC
- `SetAuditOnUpdate` define `UpdatedBy` e `UpdatedAt` em UTC
- Testes unitários validam o comportamento de auditoria
- `dotnet build` compila sem erros
