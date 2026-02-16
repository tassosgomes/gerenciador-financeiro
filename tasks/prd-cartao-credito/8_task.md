```markdown
---
status: pending
parallelizable: true
blocked_by: ["3.0", "5.0"]
---

<task_context>
<domain>application/command</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>3.0, 5.0</dependencies>
<unblocks>"9.0"</unblocks>
</task_context>

# Tarefa 8.0: Command de Pagamento de Fatura

## Visão Geral

Implementar o `PayInvoiceCommand` — operação dedicada de pagamento de fatura de cartão de crédito. O pagamento gera um par de transações vinculadas via `TransferGroupId` (débito na conta de débito do cartão, crédito no cartão), reutilizando a infra de transferência existente no `TransferDomainService`. A categoria utilizada é "Pagamento de Fatura" (seed da tarefa 5.0).

## Requisitos

- PRD F5 req 24: Pagamento gera duas transações vinculadas (Debit na conta de débito, Credit no cartão)
- PRD F5 req 25: Informar cartão, valor e data de competência
- PRD F5 req 26: Conta de débito é a vinculada no cadastro do cartão
- PRD F5 req 27: Permitir pagamento parcial
- PRD F5 req 29: Pagamento acima do valor gera saldo positivo (crédito a favor)
- PRD F5 req 30: Respeitar regras de saldo da conta de débito
- PRD F5 req 31: Registrar como transferência com `TransferGroupId` e indicação de pagamento
- Techspec: `PayInvoiceCommand(CreditCardAccountId, Amount, CompetenceDate, UserId, OperationId?)`
- Techspec: Estender `TransferDomainService` com `CreateInvoicePayment`
- Techspec: Reutilizar `GetByIdWithLockAsync` para concorrência

## Subtarefas

### Extensão de TransferDomainService

- [x] 8.1 Criar método `CreateInvoicePayment` em `TransferDomainService`:
  - Parâmetros: `Account debitAccount`, `Account creditCardAccount`, `decimal amount`, `DateTime competenceDate`, `Guid categoryId`, `string userId`, `string? operationId`
  - Reutiliza/adapta lógica de `CreateTransfer` existente
  - Diferenças em relação a transferência comum:
    - Descrição: "Pgto. Fatura — {creditCardAccount.Name}" (ao invés de "Transf.")
    - Categoria: usa a categoria seed "Pagamento de Fatura" (recebida como parâmetro)
    - Gera `TransferGroupId` novo para vincular as duas transações
  - Cria par de transações via `TransactionDomainService`:
    - `Debit` na `debitAccount` com `amount`
    - `Credit` na `creditCardAccount` com `amount`
  - Retorna `IReadOnlyList<Transaction>` (as duas transações)

### Command e Handler

- [x] 8.2 Criar `PayInvoiceCommand` em `2-Application/GestorFinanceiro.Financeiro.Application/Commands/Invoice/PayInvoiceCommand.cs`:
  ```csharp
  public record PayInvoiceCommand(
      Guid CreditCardAccountId,
      decimal Amount,
      DateTime CompetenceDate,
      string UserId,
      string? OperationId = null
  ) : ICommand<IReadOnlyList<TransactionResponse>>;
  ```

- [x] 8.3 Criar `PayInvoiceCommandHandler`:
  - Verificar idempotência via `OperationId` (padrão existente)
  - Carregar cartão com lock: `IAccountRepository.GetByIdWithLockAsync(creditCardAccountId, ct)`
  - Validar que conta é cartão (`CreditCard != null`)
  - Obter conta de débito do cartão: `account.CreditCard.DebitAccountId`
  - Carregar conta de débito com lock: `IAccountRepository.GetByIdWithLockAsync(debitAccountId, ct)`
  - Validar que conta de débito está ativa
  - Buscar categoria "Pagamento de Fatura" via `ICategoryRepository`
  - Chamar `TransferDomainService.CreateInvoicePayment(debitAccount, cardAccount, amount, competenceDate, categoryId, userId, operationId)`
  - Persistir transações
  - Registrar auditoria via `IAuditService.LogAsync`
  - Commit via `IUnitOfWork`
  - Retornar `IReadOnlyList<TransactionResponse>` mapeado

- [x] 8.4 Criar `PayInvoiceCommandValidator`:
  - `CreditCardAccountId` != Guid.Empty
  - `Amount` > 0
  - `CompetenceDate` não pode ser futura
  - `UserId` não vazio

### Testes Unitários

- [x] 8.5 Criar testes para `TransferDomainService.CreateInvoicePayment` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Service/TransferDomainServiceTests.cs`:
  - `CreateInvoicePayment_WithValidAccounts_ShouldReturnTwoTransactions`
  - `CreateInvoicePayment_ShouldCreateDebitOnDebitAccount`
  - `CreateInvoicePayment_ShouldCreateCreditOnCardAccount`
  - `CreateInvoicePayment_ShouldSetTransferGroupId`
  - `CreateInvoicePayment_ShouldUseInvoicePaymentDescription`
  - `CreateInvoicePayment_DebitAccountWithInsufficientBalance_ShouldThrow` (se AllowNegativeBalance=false)

- [x] 8.6 Criar testes para `PayInvoiceCommandHandler` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Application/Commands/PayInvoiceCommandHandlerTests.cs`:
  - `Handle_WithValidPayment_ShouldReturnTransactionResponses`
  - `Handle_AccountNotFound_ShouldThrowNotFoundException`
  - `Handle_AccountIsNotCard_ShouldThrowDomainException`
  - `Handle_DebitAccountInactive_ShouldThrowDomainException`
  - `Handle_WithOperationId_ShouldBeIdempotent`
  - `Handle_ShouldAuditLog`
  - `Handle_PartialPayment_ShouldSucceed` (valor menor que fatura)
  - `Handle_OverPayment_ShouldSucceed` (crédito a favor)

- [x] 8.7 Criar testes para `PayInvoiceCommandValidator`:
  - `Validate_WithEmptyAccountId_ShouldFail`
  - `Validate_WithZeroAmount_ShouldFail`
  - `Validate_WithValidParameters_ShouldPass`

### Validação

- [x] 8.8 Validar build com `dotnet build`
- [x] 8.9 Executar testes com `dotnet test`

## Sequenciamento

- Bloqueado por: 3.0 (TransactionDomainService com ValidateCreditLimit), 5.0 (Categoria seed + repositórios)
- Desbloqueia: 9.0 (InvoicesController POST pay)
- Paralelizável: Sim — com 6.0 e 7.0

## Detalhes de Implementação

### CreateInvoicePayment no TransferDomainService

```csharp
public IReadOnlyList<Transaction> CreateInvoicePayment(
    Account debitAccount,
    Account creditCardAccount,
    decimal amount,
    DateTime competenceDate,
    Guid categoryId,
    string userId,
    string? operationId)
{
    var transferGroupId = Guid.NewGuid();
    var description = $"Pgto. Fatura — {creditCardAccount.Name}";

    // Transação de débito na conta de débito (ex: Corrente)
    var debitTransaction = _transactionDomainService.CreateTransaction(
        debitAccount, categoryId, TransactionType.Debit, amount,
        description, competenceDate, competenceDate, userId, operationId);
    debitTransaction.SetTransferGroup(transferGroupId);

    // Transação de crédito no cartão (reduz fatura)
    var creditTransaction = _transactionDomainService.CreateTransaction(
        creditCardAccount, categoryId, TransactionType.Credit, amount,
        description, competenceDate, competenceDate, userId,
        operationId != null ? $"{operationId}-credit" : null);
    creditTransaction.SetTransferGroup(transferGroupId);

    return new List<Transaction> { debitTransaction, creditTransaction };
}
```

### Observações

- **Concorrência**: Ambas as contas são carregadas com lock (`GetByIdWithLockAsync` → `SELECT FOR UPDATE`). O cartão é lockado primeiro, depois a conta de débito (ordem consistente para evitar deadlock).
- **Categoria**: O handler busca a categoria "Pagamento de Fatura" por nome ou por flag `IsSystem` + nome. Se não encontrar (seed não executou), deve lançar exceção clara.
- **OperationId**: Para idempotência, o `OperationId` da transação de crédito deve ser diferente do débito (append `-credit`).
- **Pagamento parcial**: O amount pode ser menor que a fatura — não há validação de valor mínimo.
- **Pagamento excedente**: O amount pode ser maior que a fatura — o crédito resultante gera saldo positivo no cartão (crédito a favor, PRD Decisão 4).

## Critérios de Sucesso

- `TransferDomainService.CreateInvoicePayment` gera par de transações com `TransferGroupId`
- Descrição é "Pgto. Fatura — {nomeCartão}" (distingue de transferências comuns)
- Débito é na conta de débito vinculada ao cartão
- Crédito é no cartão (reduz fatura / gera crédito a favor)
- Handler carrega ambas as contas com lock (concorrência)
- Handler valida que conta é cartão e conta de débito está ativa
- Idempotência via `OperationId` funciona
- Auditoria registrada
- Pagamento parcial aceito sem restrições
- Pagamento excedente aceito (gera saldo positivo)
- Todos os testes passam
- Build compila sem erros
```
