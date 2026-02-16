```markdown
---
status: pending
parallelizable: false
blocked_by: []
---

<task_context>
<domain>domain/entidade</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"2.0", "3.0"</unblocks>
</task_context>

# Tarefa 1.0: Value Object CreditCardDetails e Exceções

## Visão Geral

Criar o value object `CreditCardDetails` — classe de domínio que encapsula toda a configuração específica de cartão de crédito (limite, dia de fechamento, dia de vencimento, conta de débito, flag de limite rígido). Também criar as exceções de domínio específicas para a feature. Esta é a tarefa fundacional: nenhuma dependência externa e totalmente testável unitariamente.

## Requisitos

- PRD F1 req 1: Cartão deve ter limite de crédito, dia de fechamento (1-28), dia de vencimento (1-28) e conta de débito
- PRD F1 req 4: Limite de crédito deve ser maior que zero
- PRD F1 req 5: Dias de fechamento e vencimento no intervalo 1-28
- PRD F3 req 12: Flag `EnforceCreditLimit` (padrão: true)
- Techspec: `CreditCardDetails` como value object com `Create` e `Update` + validação interna
- Techspec: Exceções `CreditLimitExceededException` e `InvalidCreditCardConfigException`
- `rules/dotnet-architecture.md`: Domain sem dependências externas
- `rules/dotnet-coding-standards.md`: Código em inglês, PascalCase

## Subtarefas

### Value Object

- [ ] 1.1 Criar `CreditCardDetails` em `3-Domain/GestorFinanceiro.Financeiro.Domain/Entity/CreditCardDetails.cs`:
  - Propriedades: `CreditLimit` (decimal), `ClosingDay` (int), `DueDay` (int), `DebitAccountId` (Guid), `EnforceCreditLimit` (bool)
  - Construtor `protected CreditCardDetails() { }` para EF Core
  - Factory method `Create(creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit)` com validações
  - Método `Update(creditLimit, closingDay, dueDay, debitAccountId, enforceCreditLimit)` com mesmas validações
  - Validações: `creditLimit <= 0` → exceção; `closingDay < 1 || > 28` → exceção; `dueDay < 1 || > 28` → exceção

### Exceções

- [ ] 1.2 Criar `CreditLimitExceededException` em `3-Domain/GestorFinanceiro.Financeiro.Domain/Exception/CreditLimitExceededException.cs`:
  - Herdar de `DomainException` (padrão existente)
  - Receber `accountId`, `availableLimit`, `requestedAmount` no construtor
  - Mensagem descritiva: "Limite de crédito excedido. Disponível: {available}, Solicitado: {amount}"
- [ ] 1.3 Criar `InvalidCreditCardConfigException` em `3-Domain/GestorFinanceiro.Financeiro.Domain/Exception/InvalidCreditCardConfigException.cs`:
  - Herdar de `DomainException`
  - Receber mensagem descritiva no construtor

### Testes Unitários

- [ ] 1.4 Criar testes para `CreditCardDetails` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Entity/CreditCardDetailsTests.cs`:
  - `Create_WithValidParameters_ShouldReturnInstance`
  - `Create_WithZeroCreditLimit_ShouldThrowInvalidCreditCardConfigException`
  - `Create_WithNegativeCreditLimit_ShouldThrowInvalidCreditCardConfigException`
  - `Create_WithClosingDayLessThan1_ShouldThrowInvalidCreditCardConfigException`
  - `Create_WithClosingDayGreaterThan28_ShouldThrowInvalidCreditCardConfigException`
  - `Create_WithDueDayLessThan1_ShouldThrowInvalidCreditCardConfigException`
  - `Create_WithDueDayGreaterThan28_ShouldThrowInvalidCreditCardConfigException`
  - `Update_WithValidParameters_ShouldUpdateProperties`
  - `Update_WithInvalidParameters_ShouldThrowException` (mesmas regras do Create)
- [ ] 1.5 Criar testes para `CreditLimitExceededException` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Exception/CreditLimitExceededExceptionTests.cs`:
  - `Constructor_ShouldContainAccountIdAndAmountsInMessage`
- [ ] 1.6 Criar testes para `InvalidCreditCardConfigException` em `5-Tests/GestorFinanceiro.Financeiro.UnitTests/Domain/Exception/InvalidCreditCardConfigExceptionTests.cs`:
  - `Constructor_ShouldContainCustomMessage`

### Validação

- [ ] 1.7 Validar build com `dotnet build` a partir de `backend/`

## Sequenciamento

- Bloqueado por: Nenhum (primeira tarefa)
- Desbloqueia: 2.0 (Extensão de Account), 3.0 (CreditCardDomainService)
- Paralelizável: Não (é a tarefa fundacional)

## Detalhes de Implementação

### Value Object CreditCardDetails (conforme techspec)

```csharp
public class CreditCardDetails
{
    public decimal CreditLimit { get; private set; }
    public int ClosingDay { get; private set; }       // 1-28
    public int DueDay { get; private set; }            // 1-28
    public Guid DebitAccountId { get; private set; }   // FK → Account (Corrente/Carteira)
    public bool EnforceCreditLimit { get; private set; }

    protected CreditCardDetails() { }  // EF Core

    public static CreditCardDetails Create(
        decimal creditLimit,
        int closingDay,
        int dueDay,
        Guid debitAccountId,
        bool enforceCreditLimit)
    {
        if (creditLimit <= 0)
            throw new InvalidCreditCardConfigException("Limite de crédito deve ser maior que zero.");
        if (closingDay < 1 || closingDay > 28)
            throw new InvalidCreditCardConfigException("Dia de fechamento deve estar entre 1 e 28.");
        if (dueDay < 1 || dueDay > 28)
            throw new InvalidCreditCardConfigException("Dia de vencimento deve estar entre 1 e 28.");

        return new CreditCardDetails
        {
            CreditLimit = creditLimit,
            ClosingDay = closingDay,
            DueDay = dueDay,
            DebitAccountId = debitAccountId,
            EnforceCreditLimit = enforceCreditLimit
        };
    }

    public void Update(
        decimal creditLimit,
        int closingDay,
        int dueDay,
        Guid debitAccountId,
        bool enforceCreditLimit)
    {
        if (creditLimit <= 0)
            throw new InvalidCreditCardConfigException("Limite de crédito deve ser maior que zero.");
        if (closingDay < 1 || closingDay > 28)
            throw new InvalidCreditCardConfigException("Dia de fechamento deve estar entre 1 e 28.");
        if (dueDay < 1 || dueDay > 28)
            throw new InvalidCreditCardConfigException("Dia de vencimento deve estar entre 1 e 28.");

        CreditLimit = creditLimit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        DebitAccountId = debitAccountId;
        EnforceCreditLimit = enforceCreditLimit;
    }
}
```

### Exceções (conforme padrão existente)

```csharp
public class CreditLimitExceededException : DomainException
{
    public CreditLimitExceededException(Guid accountId, decimal availableLimit, decimal requestedAmount)
        : base($"Limite de crédito excedido na conta {accountId}. Disponível: {availableLimit:C}, Solicitado: {requestedAmount:C}")
    { }
}

public class InvalidCreditCardConfigException : DomainException
{
    public InvalidCreditCardConfigException(string message) : base(message) { }
}
```

### Padrão de Testes (conforme existente)

```csharp
[Fact]
public void Create_WithValidParameters_ShouldReturnInstance()
{
    // Arrange
    var creditLimit = 5000m;
    var closingDay = 3;
    var dueDay = 10;
    var debitAccountId = Guid.NewGuid();

    // Act
    var details = CreditCardDetails.Create(creditLimit, closingDay, dueDay, debitAccountId, true);

    // Assert
    details.CreditLimit.Should().Be(creditLimit);
    details.ClosingDay.Should().Be(closingDay);
    details.DueDay.Should().Be(dueDay);
    details.DebitAccountId.Should().Be(debitAccountId);
    details.EnforceCreditLimit.Should().BeTrue();
}

[Theory]
[InlineData(0)]
[InlineData(-100)]
public void Create_WithInvalidCreditLimit_ShouldThrowInvalidCreditCardConfigException(decimal creditLimit)
{
    // Act
    var act = () => CreditCardDetails.Create(creditLimit, 3, 10, Guid.NewGuid(), true);

    // Assert
    act.Should().Throw<InvalidCreditCardConfigException>();
}
```

## Critérios de Sucesso

- `CreditCardDetails` compila e segue o padrão de value object (sem herança de `BaseEntity`, sem Id)
- `Create` com parâmetros válidos retorna instância com propriedades preenchidas
- `Create` com `creditLimit ≤ 0` lança `InvalidCreditCardConfigException`
- `Create` com `closingDay` ou `dueDay` fora de 1-28 lança `InvalidCreditCardConfigException`
- `Update` com parâmetros válidos atualiza propriedades
- `Update` com parâmetros inválidos lança exceção
- Exceções herdam de `DomainException`
- Todos os testes unitários passam
- Build compila sem erros
```
