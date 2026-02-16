```markdown
---
status: pending
parallelizable: false
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

# Tarefa 6.0: Commands de Conta Adaptados para Cartão de Crédito

## Visão Geral

Adaptar os commands de criação e edição de conta para suportar o tipo Cartão de Crédito. O `CreateAccountCommandHandler` deve bifurcar: quando `type == Cartao`, usa `Account.CreateCreditCard(...)` com os campos específicos de cartão e ignora `initialBalance`/`allowNegativeBalance`; para demais tipos, mantém o fluxo existente inalterado. O `UpdateAccountCommandHandler` deve chamar `UpdateCreditCard(...)` quando a conta é tipo Cartão. Os validators devem ter regras condicionais por tipo.

## Requisitos

- PRD F1 req 1-7: Cadastro diferenciado de cartão de crédito
- PRD F2 req 8-11: Edição de cartão de crédito
- PRD F1 req 2: Saldo do cartão sempre inicia em 0
- PRD F1 req 3: Saldo negativo sempre permitido para cartão
- PRD F1 req 6: Conta de débito deve ser ativa e do tipo Corrente ou Carteira
- Techspec: `CreateAccountCommand` estendido com campos de cartão opcionais
- Techspec: Bifurcação no handler por `AccountType`
- Techspec: Validators com `When(x => x.Type == AccountType.Cartao, ...)`
- `rules/dotnet-architecture.md`: Application orquestra Domain, sem lógica de negócio

## Subtarefas

### Extensão de Commands

- [x] 6.1 Estender `CreateAccountCommand` com campos de cartão opcionais:
  - `decimal? CreditLimit`
  - `int? ClosingDay`
  - `int? DueDay`
  - `Guid? DebitAccountId`
  - `bool? EnforceCreditLimit`

- [x] 6.2 Estender `UpdateAccountCommand` com campos de cartão opcionais:
  - Mesmos campos opcionais do create

### Extensão de Validators

- [x] 6.3 Estender `CreateAccountCommandValidator` com regras condicionais:
  - `When(x => x.Type == AccountType.Cartao, () => { ... })`:
    - `CreditLimit` obrigatório e > 0
    - `ClosingDay` obrigatório, entre 1 e 28
    - `DueDay` obrigatório, entre 1 e 28
    - `DebitAccountId` obrigatório e != Guid.Empty
    - `EnforceCreditLimit` opcionalmente aceita default `true`
  - `When(x => x.Type != AccountType.Cartao, () => { ... })`:
    - Manter validações existentes de `InitialBalance`, `AllowNegativeBalance`

- [x] 6.4 Estender `UpdateAccountCommandValidator` com regras condicionais similares

### Extensão de Handlers

- [x] 6.5 Estender `CreateAccountCommandHandler`:
  - Manter fluxo existente para idempotência (`OperationId`) e verificação de nome único
  - **Bifurcação**: se `command.Type == AccountType.Cartao`:
    - Validar que `DebitAccountId` aponta para conta ativa do tipo Corrente ou Carteira (via `IAccountRepository.GetByIdAsync`)
    - Chamar `Account.CreateCreditCard(name, creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit, userId)`
  - Senão: manter `Account.Create(name, type, initialBalance, allowNegativeBalance, userId)` inalterado
  - Persistir, auditar (via `IAuditService.LogAsync`), commit

- [x] 6.6 Estender `UpdateAccountCommandHandler`:
  - **Bifurcação**: se conta é tipo Cartão (`account.CreditCard != null`):
    - Validar que `DebitAccountId` aponta para conta ativa do tipo Corrente ou Carteira
    - Chamar `account.UpdateCreditCard(name, creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit, userId)`
  - Senão: manter `account.Update(name, allowNegativeBalance, userId)` inalterado
  - Persistir, auditar, commit

### Extensão de DTOs

- [x] 6.7 Criar `CreditCardDetailsResponse` em DTOs:
  ```csharp
  public record CreditCardDetailsResponse(
      decimal CreditLimit,
      int ClosingDay,
      int DueDay,
      Guid DebitAccountId,
      bool EnforceCreditLimit,
      decimal AvailableLimit
  );
  ```

- [x] 6.8 Estender `AccountResponse` com campo `CreditCard?`:
  ```csharp
  public record AccountResponse(
      // ... campos existentes ...
      CreditCardDetailsResponse? CreditCard  // null para não-cartão
  );
  ```

- [x] 6.9 Atualizar mapeamento Mapster (se usado) para incluir `CreditCardDetails` → `CreditCardDetailsResponse`:
  - `AvailableLimit` é campo calculado: `account.GetAvailableLimit()`

### Testes Unitários

- [x] 6.10 Testes para `CreateAccountCommandHandler`:
  - `Handle_WithTypeCarto_ShouldCreateCreditCardAccount`
  - `Handle_WithTypeCarto_ShouldSetBalanceToZero`
  - `Handle_WithTypeCarto_ShouldSetAllowNegativeBalanceToTrue`
  - `Handle_WithTypeCarto_InvalidDebitAccountId_ShouldThrow` (conta não encontrada)
  - `Handle_WithTypeCarto_DebitAccountInactive_ShouldThrow`
  - `Handle_WithTypeCarto_DebitAccountIsInvestimento_ShouldThrow` (tipo inválido)
  - `Handle_WithTypeCorrente_ShouldMaintainExistingBehavior` (regressão)
  - `Handle_WithTypeCarto_ShouldAuditLog`

- [x] 6.11 Testes para `UpdateAccountCommandHandler`:
  - `Handle_UpdateCreditCard_ShouldUpdateAllFields`
  - `Handle_UpdateCreditCard_InvalidDebitAccount_ShouldThrow`
  - `Handle_UpdateRegularAccount_ShouldMaintainExistingBehavior` (regressão)
  - `Handle_UpdateCreditCard_ShouldAuditLog`

- [x] 6.12 Testes para validators:
  - `CreateAccountCommandValidator_TypeCarto_MissingCreditLimit_ShouldFail`
  - `CreateAccountCommandValidator_TypeCarto_ValidFields_ShouldPass`
  - `CreateAccountCommandValidator_TypeCorrente_WithoutCreditFields_ShouldPass`

### Validação

- [x] 6.13 Validar build com `dotnet build`
- [x] 6.14 Executar testes com `dotnet test`

## Sequenciamento

- Bloqueado por: 3.0 (Domain completo com ValidateCreditLimit), 5.0 (Repositórios com GetActiveByTypeAsync)
- Desbloqueia: 9.0 (API Controllers)
- Paralelizável: Com 7.0 e 8.0 (parcialmente — compartilham dependência de 5.0)

## Detalhes de Implementação

### Bifurcação no CreateAccountCommandHandler

```csharp
// Dentro do Handle:
Account account;
if (command.Type == AccountType.Cartao)
{
    // Validar conta de débito
    var debitAccount = await _accountRepository.GetByIdAsync(command.DebitAccountId!.Value, ct);
    if (debitAccount == null)
        throw new NotFoundException("Conta de débito não encontrada.");
    if (!debitAccount.IsActive)
        throw new DomainException("Conta de débito está inativa.");
    if (debitAccount.Type != AccountType.Corrente && debitAccount.Type != AccountType.Carteira)
        throw new DomainException("Conta de débito deve ser do tipo Corrente ou Carteira.");

    account = Account.CreateCreditCard(
        command.Name,
        command.CreditLimit!.Value,
        command.ClosingDay!.Value,
        command.DueDay!.Value,
        command.DebitAccountId!.Value,
        command.EnforceCreditLimit ?? true,
        command.UserId);
}
else
{
    // Fluxo existente inalterado
    account = Account.Create(
        command.Name, command.Type,
        command.InitialBalance, command.AllowNegativeBalance,
        command.UserId);
}
```

### Validator Condicional

```csharp
public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        // ... regras existentes ...

        When(x => x.Type == AccountType.Cartao, () =>
        {
            RuleFor(x => x.CreditLimit)
                .NotNull().WithMessage("Limite de crédito é obrigatório para cartão.")
                .GreaterThan(0).WithMessage("Limite deve ser maior que zero.");
            RuleFor(x => x.ClosingDay)
                .NotNull().WithMessage("Dia de fechamento é obrigatório.")
                .InclusiveBetween(1, 28).WithMessage("Dia de fechamento deve estar entre 1 e 28.");
            RuleFor(x => x.DueDay)
                .NotNull().WithMessage("Dia de vencimento é obrigatório.")
                .InclusiveBetween(1, 28).WithMessage("Dia de vencimento deve estar entre 1 e 28.");
            RuleFor(x => x.DebitAccountId)
                .NotNull().NotEmpty().WithMessage("Conta de débito é obrigatória.");
        });
    }
}
```

### Observações

- **Mapeamento Mapster**: Verificar se o projeto usa Mapster configuração global ou inline. O campo `AvailableLimit` é calculado por `account.GetAvailableLimit()` e não mapeado automaticamente.
- **Idempotência**: O `OperationId` existente no `CreateAccountCommand` deve continuar funcionando para cartões.
- **Auditoria**: `IAuditService.LogAsync` deve registrar a criação/edição com os campos de cartão.

## Critérios de Sucesso

- `CreateAccountCommand` aceita campos de cartão opcionais
- Handler bifurca corretamente por tipo: `Account.CreateCreditCard` para Cartão, `Account.Create` para demais
- Cartão é criado com `Balance=0`, `AllowNegativeBalance=true`
- Conta de débito é validada (ativa, tipo Corrente ou Carteira)
- `AccountResponse` inclui `CreditCardDetailsResponse?` nullable
- `AvailableLimit` é calculado corretamente no response
- Validators têm regras condicionais por tipo
- Fluxo para contas Corrente/Investimento/Carteira permanece inalterado (regressão zero)
- Todos os testes unitários passam (novos e existentes)
- Build compila sem erros
```
