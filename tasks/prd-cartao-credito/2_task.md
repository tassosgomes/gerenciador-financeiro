```markdown
---
status: pending
parallelizable: false
blocked_by: ["1.0"]
---

<task_context>
<domain>domain/entidade</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>1.0</dependencies>
<unblocks>"3.0", "4.0"</unblocks>
</task_context>

# Tarefa 2.0: Extensão da Entidade Account

## Visão Geral

Estender a entidade `Account` para suportar composição com `CreditCardDetails` — adicionar propriedade nullable `CreditCard?`, factory method `CreateCreditCard(...)`, método `UpdateCreditCard(...)`, `ValidateCreditLimit(...)` e `GetAvailableLimit()`. A entidade continua funcionando normalmente para os demais tipos (Corrente, Investimento, Carteira) — as novas propriedades e métodos são opcionais e protegidos por null-check.

## Requisitos

- PRD F1 req 2: Saldo do cartão sempre inicia em 0
- PRD F1 req 3: Saldo negativo sempre permitido implicitamente para cartão
- PRD F2 req 8: Editar limite, fechamento, vencimento, conta de débito, flag de limite rígido
- PRD F3 req 13: Se `EnforceCreditLimit=true`, rejeitar débito acima do limite
- PRD F3 req 14: Se `EnforceCreditLimit=false`, aceitar transação mas exibir alerta
- PRD F3 req 15: Limite disponível = `CreditLimit - |SaldoAtual|`
- Techspec: Propriedade `CreditCard?` nullable (null para Corrente/Investimento/Carteira)
- Techspec: `CreateCreditCard` força `Balance=0`, `AllowNegativeBalance=true`
- Techspec: `ValidateCreditLimit` verifica `CreditCard != null` e `EnforceCreditLimit` antes de lançar exceção
- Techspec: `GetAvailableLimit` retorna 0 para contas sem `CreditCard`
- `rules/dotnet-coding-standards.md`: Encapsulamento — setters privados, factory methods

## Subtarefas

### Extensão de Account

- [ ] 2.1 Adicionar propriedade `CreditCard` (`CreditCardDetails?`, nullable) em `Account.cs`:
  - Setter privado
  - Property é `null` para contas dos tipos Corrente, Investimento e Carteira

- [ ] 2.2 Criar factory method `CreateCreditCard(...)` em `Account.cs`:
  - Parâmetros: `name`, `creditLimit`, `closingDay`, `dueDay`, `debitAccountId`, `enforceCreditLimit`, `userId`
  - Força: `Balance = 0`, `AllowNegativeBalance = true`, `Type = AccountType.Cartao`
  - Delega criação de `CreditCardDetails` ao factory method `CreditCardDetails.Create(...)`
  - Chama `SetAuditOnCreate(userId)`

- [ ] 2.3 Criar método `UpdateCreditCard(...)` em `Account.cs`:
  - Parâmetros: `name`, `creditLimit`, `closingDay`, `dueDay`, `debitAccountId`, `enforceCreditLimit`, `userId`
  - Valida que `CreditCard != null` (senão `InvalidCreditCardConfigException`)
  - Atualiza `Name` diretamente
  - Delega atualização de campos de cartão para `CreditCard.Update(...)`
  - Chama `SetAuditOnUpdate(userId)`

- [ ] 2.4 Criar método `ValidateCreditLimit(decimal amount)` em `Account.cs`:
  - Se `CreditCard == null` → retorna (bypass — não é cartão)
  - Se `!CreditCard.EnforceCreditLimit` → retorna (limite informativo)
  - Se `GetAvailableLimit() < amount` → lança `CreditLimitExceededException(Id, GetAvailableLimit(), amount)`

- [ ] 2.5 Criar método `GetAvailableLimit()` em `Account.cs`:
  - Se `CreditCard == null` → retorna 0
  - Retorna `CreditCard.CreditLimit - Math.Abs(Balance)`

### Testes Unitários

- [ ] 2.6 Criar/estender testes em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Entity/AccountTests.cs`:
  - `CreateCreditCard_WithValidParameters_ShouldSetBalanceToZero`
  - `CreateCreditCard_WithValidParameters_ShouldSetAllowNegativeBalanceToTrue`
  - `CreateCreditCard_WithValidParameters_ShouldSetTypeToCarto`
  - `CreateCreditCard_WithValidParameters_ShouldHaveCreditCardDetailsPopulated`
  - `CreateCreditCard_WithInvalidCreditLimit_ShouldThrowException` (delega para CreditCardDetails)
  - `UpdateCreditCard_WithValidParameters_ShouldUpdateNameAndDetails`
  - `UpdateCreditCard_WhenNotCreditCard_ShouldThrowInvalidCreditCardConfigException`
  - `UpdateCreditCard_ShouldUpdateAudit`
  - `ValidateCreditLimit_WhenCreditCardIsNull_ShouldNotThrow`
  - `ValidateCreditLimit_WhenEnforceIsFalse_ShouldNotThrow`
  - `ValidateCreditLimit_WhenAmountExceedsLimit_ShouldThrowCreditLimitExceededException`
  - `ValidateCreditLimit_WhenAmountWithinLimit_ShouldNotThrow`
  - `GetAvailableLimit_WhenCreditCardIsNull_ShouldReturnZero`
  - `GetAvailableLimit_WithNegativeBalance_ShouldReturnLimitMinusAbsBalance`
  - `GetAvailableLimit_WithPositiveBalance_ShouldReturnLimitPlusBalance`

### Validação

- [ ] 2.7 Validar build com `dotnet build` a partir de `backend/`
- [ ] 2.8 Executar testes unitários com `dotnet test` para o projeto de testes unitários

## Sequenciamento

- Bloqueado por: 1.0 (CreditCardDetails e Exceções devem existir)
- Desbloqueia: 3.0 (CreditCardDomainService), 4.0 (Migration EF Core)
- Paralelizável: Não

## Detalhes de Implementação

### Extensão de Account (conforme techspec)

```csharp
public class Account : BaseEntity
{
    // ... campos existentes inalterados ...

    // Owned entity — null para Corrente/Investimento/Carteira
    public CreditCardDetails? CreditCard { get; private set; }

    public static Account CreateCreditCard(
        string name,
        decimal creditLimit,
        int closingDay,
        int dueDay,
        Guid debitAccountId,
        bool enforceCreditLimit,
        string userId)
    {
        var account = new Account
        {
            Name = name,
            Type = AccountType.Cartao,
            Balance = 0,                    // Cartão sempre inicia com saldo 0
            AllowNegativeBalance = true,     // Compras geram saldo negativo
            CreditCard = CreditCardDetails.Create(
                creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit)
        };
        account.SetAuditOnCreate(userId);
        return account;
    }

    public void UpdateCreditCard(
        string name,
        decimal creditLimit,
        int closingDay,
        int dueDay,
        Guid debitAccountId,
        bool enforceCreditLimit,
        string userId)
    {
        if (CreditCard == null)
            throw new InvalidCreditCardConfigException("Conta não é um cartão de crédito.");

        Name = name;
        CreditCard.Update(creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit);
        SetAuditOnUpdate(userId);
    }

    public decimal GetAvailableLimit()
    {
        if (CreditCard == null) return 0;
        return CreditCard.CreditLimit - Math.Abs(Balance);
    }

    public void ValidateCreditLimit(decimal amount)
    {
        if (CreditCard == null) return;
        if (!CreditCard.EnforceCreditLimit) return;
        if (GetAvailableLimit() < amount)
            throw new CreditLimitExceededException(Id, GetAvailableLimit(), amount);
    }
}
```

### Observações

- **Não modificar** os métodos existentes `Create`, `Restore`, `Update`, `ApplyDebit`, `ApplyCredit`, `RevertDebit`, `RevertCredit`, `ValidateCanReceiveTransaction`
- O `ApplyDebit` continuará funcionando normalmente — a validação de limite será chamada externamente pelo `TransactionDomainService` (tarefa 3.0)
- O `Restore` existente precisará ser avaliado: se o EF Core restaura owned entities automaticamente via `Restore`, nenhuma mudança é necessária. Caso contrário, será necessário um overload de `Restore` que aceite `CreditCardDetails?`

## Critérios de Sucesso

- `Account.CreditCard` é nullable e `null` por padrão (contas existentes não afetadas)
- `CreateCreditCard` gera conta com `Balance=0`, `AllowNegativeBalance=true`, `Type=Cartao`
- `CreateCreditCard` com parâmetros inválidos delega exceção para `CreditCardDetails.Create`
- `UpdateCreditCard` em conta sem `CreditCard` lança exceção
- `ValidateCreditLimit` com limite rígido e compra acima do limite lança `CreditLimitExceededException`
- `ValidateCreditLimit` com limite informativo (`EnforceCreditLimit=false`) não lança exceção
- `ValidateCreditLimit` para conta não-cartão (`CreditCard==null`) é bypass silencioso
- `GetAvailableLimit` retorna 0 para contas sem cartão
- `GetAvailableLimit` calcula corretamente `CreditLimit - |Balance|`
- Todos os testes unitários existentes continuam passando (regressão zero)
- Novos testes unitários passam
- Build compila sem erros
```
