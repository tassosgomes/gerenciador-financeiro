---
status: pending
parallelizable: false
blocked_by: ["2.0"]
---

<task_context>
<domain>engine/domínio</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"4.0", "5.0"</unblocks>
</task_context>

# Tarefa 3.0: Domain Layer — Entidades e Exceções

## Visão Geral

Implementar todas as entidades de domínio (`Account`, `Category`, `Transaction`, `RecurrenceTemplate`, `OperationLog`) e as exceções de domínio. As entidades encapsulam lógica de negócio pura — factory methods, validações, alterações de estado — e são totalmente testáveis sem mocks.

Esta é a tarefa mais complexa do domínio, pois envolve as regras fundamentais do engine financeiro: saldo materializado, cancelamento lógico, ajuste por diferença contábil, parcelamento, recorrência e transferência.

## Requisitos

- PRD F1 req 1–7: entidade `Account` com saldo materializado, tipos, ativação/inativação, validação de saldo negativo
- PRD F2 req 8–12: entidade `Category` com nome e tipo, edição de nome
- PRD F3 req 13–19: entidade `Transaction` com todos os campos, factory method, `IsOverdue` calculado on-the-fly
- PRD F4 req 20–23: factory method `CreateAdjustment`, `MarkAsAdjusted`
- PRD F5 req 24–27: método `Cancel` com registro de motivo
- PRD F6 req 28–30: helpers `SetInstallmentInfo`
- PRD F7 req 32–35: entidade `RecurrenceTemplate` com `ShouldGenerateForMonth`
- PRD F8 req 36–37: helper `SetTransferGroup`
- PRD F9 req 40–42: auditoria via `BaseEntity`
- PRD F10 req 43–46: entidade `OperationLog` para idempotência
- Exceções de domínio para cada cenário de erro

## Subtarefas

- [ ] 3.1 Criar entidade `Account` com factory method `Create`, métodos `Activate/Deactivate`, `ApplyDebit/ApplyCredit`, `RevertDebit/RevertCredit`, `ValidateCanReceiveTransaction`
- [ ] 3.2 Criar entidade `Category` com factory method `Create`, método `UpdateName`
- [ ] 3.3 Criar entidade `Transaction` com factory methods `Create` e `CreateAdjustment`, método `Cancel`, `MarkAsAdjusted`, helpers `SetInstallmentInfo`, `SetRecurrenceInfo`, `SetTransferGroup`, propriedade calculada `IsOverdue`
- [ ] 3.4 Criar entidade `RecurrenceTemplate` com factory method `Create`, métodos `Deactivate`, `MarkGenerated`, `ShouldGenerateForMonth`
- [ ] 3.5 Criar classe `OperationLog` (entidade simples, sem herança de BaseEntity)
- [ ] 3.6 Criar exceções de domínio: `DomainException` (base abstrata), `InsufficientBalanceException`, `InactiveAccountException`, `InvalidTransactionAmountException`, `TransactionAlreadyCancelledException`, `TransactionNotPendingException`, `DuplicateOperationException`, `InstallmentPaidCannotBeCancelledException`
- [ ] 3.7 Testes unitários para `Account` (criação, ativar/inativar, débito/crédito, saldo negativo, conta inativa)
- [ ] 3.8 Testes unitários para `Transaction` (criação, cancelamento, ajuste, `IsOverdue`, parcela, recorrência, transferência)
- [ ] 3.9 Testes unitários para `Category` (criação, edição de nome)
- [ ] 3.10 Testes unitários para `RecurrenceTemplate` (criação, `ShouldGenerateForMonth`, desativação)

## Sequenciamento

- Bloqueado por: 2.0 (depende de Enums e BaseEntity)
- Desbloqueia: 4.0 (Interfaces de Repositório referenciam entidades), 5.0 (Domain Services operam sobre entidades)
- Paralelizável: Não (pré-requisito de quase todas as tarefas subsequentes)

## Detalhes de Implementação

### Localização dos arquivos

```
3-Domain/GestorFinanceiro.Financeiro.Domain/
├── Entity/
│   ├── BaseEntity.cs        (já existe da tarefa 2.0)
│   ├── Account.cs
│   ├── Category.cs
│   ├── Transaction.cs
│   ├── RecurrenceTemplate.cs
│   └── OperationLog.cs
└── Exception/
    ├── DomainException.cs
    ├── InsufficientBalanceException.cs
    ├── InactiveAccountException.cs
    ├── InvalidTransactionAmountException.cs
    ├── TransactionAlreadyCancelledException.cs
    ├── TransactionNotPendingException.cs
    ├── DuplicateOperationException.cs
    └── InstallmentPaidCannotBeCancelledException.cs
```

### Account — pontos-chave

- `ApplyDebit`: valida `AllowNegativeBalance` antes de debitar. Lança `InsufficientBalanceException` se saldo ficaria negativo
- `ValidateCanReceiveTransaction`: lança `InactiveAccountException` se conta está inativa
- Factory method `Create` chama `SetAuditOnCreate` internamente

### Transaction — pontos-chave

- `Amount` deve ser sempre > 0. Factory lança `InvalidTransactionAmountException` se ≤ 0
- `IsOverdue` é propriedade calculada: `Status == Pending && DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date`
- `Cancel` lança `TransactionAlreadyCancelledException` se status já é `Cancelled`
- `CreateAdjustment` define `IsAdjustment = true` e referencia a transação original via `OriginalTransactionId`

### RecurrenceTemplate — pontos-chave

- `ShouldGenerateForMonth`: retorna `true` se `IsActive` e o mês de referência é posterior ao `LastGeneratedDate`
- Dia do mês normalizado com `Math.Min(dayOfMonth, DaysInMonth)` — tratado na geração (Domain Service)

### OperationLog

- NÃO herda de `BaseEntity` — é uma entidade técnica simples
- `ExpiresAt` = `CreatedAt + 24h` (TTL de idempotência)

### Testes unitários — convenção de nomenclatura

```
MetodoTestado_Cenario_ResultadoEsperado
```

Exemplos:
- `ApplyDebit_SaldoInsuficienteSemPermissao_LancaInsufficientBalanceException`
- `Cancel_TransacaoJaCancelada_LancaTransactionAlreadyCancelledException`
- `IsOverdue_PendingComDueDatePassada_RetornaTrue`
- `ShouldGenerateForMonth_MesJaGerado_RetornaFalse`

## Critérios de Sucesso

- Todas as 5 entidades implementadas conforme a techspec
- Todas as 7 exceções de domínio implementadas
- `Account.ApplyDebit` rejeita saldo negativo quando `AllowNegativeBalance = false`
- `Transaction.Create` rejeita `amount ≤ 0`
- `Transaction.Cancel` rejeita cancelamento de transação já cancelada
- `Transaction.IsOverdue` calcula corretamente baseado em status e `DueDate`
- `RecurrenceTemplate.ShouldGenerateForMonth` retorna corretamente para meses já gerados e não gerados
- Cobertura de testes ≥ 90% para todas as entidades
- `dotnet build` compila sem erros
- `dotnet test` passa sem falhas
