# Análise Detalhada — Modelo de Contas (Account)

## 1. Situação Atual

### 1.1 Entidade de Domínio (`Account.cs`)

A entidade `Account` é **completamente genérica** — todos os tipos de conta (Corrente, Cartão, Investimento, Carteira) compartilham **exatamente a mesma estrutura**:

```csharp
public class Account : BaseEntity
{
    public string Name { get; private set; }
    public AccountType Type { get; private set; }       // Corrente=1, Cartao=2, Investimento=3, Carteira=4
    public decimal Balance { get; private set; }         // Saldo materializado
    public bool AllowNegativeBalance { get; private set; }
    public bool IsActive { get; private set; } = true;
}
```

**Não há nenhuma propriedade específica** por tipo de conta. Todos os tipos são tratados identicamente.

### 1.2 Factory Method (`Create`)

```csharp
public static Account Create(string name, AccountType type, decimal initialBalance,
    bool allowNegativeBalance, string userId)
```

Todos os tipos recebem os mesmos parâmetros: `nome`, `tipo`, `saldo inicial`, `permite saldo negativo`.

### 1.3 Tabela no Banco de Dados (`accounts`)

| Coluna                 | Tipo                     | Nullable |
|------------------------|--------------------------|----------|
| id                     | uuid                     | NOT NULL |
| name                   | varchar(150)             | NOT NULL |
| type                   | smallint                 | NOT NULL |
| balance                | numeric(18,2)            | NOT NULL |
| allow_negative_balance | boolean                  | NOT NULL |
| is_active              | boolean                  | NOT NULL |
| created_by             | varchar(100)             | NOT NULL |
| created_at             | timestamp with time zone | NOT NULL |
| updated_by             | varchar(100)             |          |
| updated_at             | timestamp with time zone |          |

**Nenhuma coluna específica** para cartão de crédito (limite, data de fechamento, data de vencimento, conta de débito).

### 1.4 API de Criação (`CreateAccountCommand`)

```csharp
public record CreateAccountCommand(
    string Name,
    AccountType Type,
    decimal InitialBalance,
    bool AllowNegativeBalance,
    string UserId,
    string? OperationId = null
) : ICommand<AccountResponse>;
```

O validador (`CreateAccountCommandValidator`) exige:
- Nome: 3-100 caracteres
- Tipo: valor válido do enum
- Saldo inicial: ≥ 0
- UserId: obrigatório

**Nenhuma validação condicional** por tipo de conta.

### 1.5 API Response (`AccountResponse`)

```csharp
public record AccountResponse(
    Guid Id, string Name, AccountType Type, decimal Balance,
    bool AllowNegativeBalance, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt
);
```

Retorna os mesmos campos para todos os tipos — inclui `Balance` mas não retorna limite, fechamento ou vencimento.

### 1.6 Frontend — Formulário de Cadastro (`AccountForm.tsx`)

O formulário é **idêntico** para todos os tipos de conta:
- Campo: Nome da Conta
- Select: Tipo de Conta (Corrente, Cartão, Investimento, Carteira)
- Input: **Saldo Inicial** (sempre exibido, independente do tipo)
- Switch: Permitir Saldo Negativo

Quando o usuário seleciona "Cartão", o formulário **não muda** — continua pedindo "Saldo Inicial" ao invés de "Limite de Crédito", e não apresenta campos de fechamento, vencimento ou conta de débito.

### 1.7 Frontend — Card de Exibição (`AccountCard.tsx`)

O card exibe:
- Ícone e cor por tipo (Cartão = roxo, ícone `credit_card`)
- **"Saldo Atual"** — mostra o `balance` da conta
- Badge "Permite saldo negativo"

Para cartão de crédito, o card deveria exibir: limite total, limite disponível, fatura atual, data de fechamento e vencimento.

### 1.8 Dashboard — Dívida de Cartão

O dashboard calcula "Dívida de cartão total" como:
```csharp
.Where(a => a.Type == AccountType.Cartao && a.Balance < 0)
.SumAsync(a => a.Balance)
```

Ou seja, simplesmente soma os saldos negativos das contas do tipo `Cartao`. Isso funciona de forma rudimentar, mas não reflete o conceito real de fatura de cartão.

---

## 2. O que Está Errado / Faltando para Cartão de Crédito

### 2.1 Propriedades Ausentes na Entidade

| Propriedade          | Descrição                                              | Tipo sugerido |
|----------------------|--------------------------------------------------------|---------------|
| `CreditLimit`        | Limite total do cartão                                 | `decimal`     |
| `ClosingDay`         | Dia do mês em que a fatura fecha                       | `int` (1-31)  |
| `DueDay`             | Dia do mês em que a fatura vence                       | `int` (1-31)  |
| `DebitAccountId`     | Conta corrente/carteira vinculada para pagamento       | `Guid?`       |

### 2.2 Problemas de Semântica

| Problema | Atual | Esperado |
|----------|-------|----------|
| **Saldo inicial** é pedido para cartão | Sim — pede "Saldo Inicial" | Não deveria pedir saldo inicial. Cartão começa com saldo 0 (sem fatura). O campo relevante é o **Limite de Crédito** |
| **AllowNegativeBalance** não faz sentido para cartão | Sim — toggle exibido | Cartão de crédito **sempre** permite saldo negativo (compras geram saldo negativo, que é a fatura). Esse campo deveria ser automaticamente `true` e oculto |
| **Sem data de fechamento** | Nenhuma | Necessário para agrupar transações na fatura do mês |
| **Sem data de vencimento** | Nenhuma | Necessário para saber quando a fatura deve ser paga |
| **Sem conta de débito** | Nenhuma | Necessário para vincular qual conta corrente/carteira paga a fatura |
| **Sem limite de crédito** | Nenhuma | Necessário para calcular "limite disponível" e validar novas compras |

### 2.3 Impacto nos Comportamentos de Domínio

- **`ApplyDebit`**: Para cartão de crédito, um débito (compra) deveria verificar se o valor excede o limite disponível (`CreditLimit - |Balance|`), não se o saldo ficaria negativo
- **Validação de limite**: Compra no cartão = `Balance - amount` < `-CreditLimit`? → rejeitar
- **Cálculo de fatura**: Agrupar transações entre a data de fechamento do mês anterior e a atual
- **Pagamento de fatura**: Debitar a conta de débito vinculada e creditar o cartão

---

## 3. Comparação: Comportamento Esperado por Tipo de Conta

| Aspecto | Corrente | Carteira | Investimento | Cartão de Crédito |
|---------|----------|----------|--------------|-------------------|
| **Saldo Inicial** | Sim | Sim | Sim | **Não** (começa em 0) |
| **Limite de Crédito** | Não | Não | Não | **Sim** |
| **Dia de Fechamento** | Não | Não | Não | **Sim** |
| **Dia de Vencimento** | Não | Não | Não | **Sim** |
| **Conta de Débito** | Não | Não | Não | **Sim** (para pagar fatura) |
| **Permitir Saldo Negativo** | Configurável | Configurável | Configurável | **Sempre true** (implícito) |
| **Saldo esperado** | ≥ 0 (ou negativo se permitido) | ≥ 0 | ≥ 0 | ≤ 0 (negativo = fatura) |
| **Débito = ?** | Gasto (diminui saldo) | Gasto (diminui saldo) | Resgate | Compra (gera fatura) |
| **Crédito = ?** | Receita (aumenta saldo) | Receita (entrada) | Aporte | Pagamento de fatura |

---

## 4. PRD — Decisões Existentes

O PRD do Core Financeiro (Fase 1) já reconhecia essas limitações:

> **Decisão 4 — Conta Cartão / fatura**: "Pagamento de fatura é feito via transferência entre contas (cartão → corrente). Conceito de 'fechamento de fatura' fica para v1.2+."

> **Não-Objetivo explícito**: "Limite automático de cartão de crédito"

Portanto, o tratamento genérico do cartão de crédito foi uma **decisão consciente** para o MVP:
- Cartão funciona como uma conta normal com saldo negativo
- Pagamento de fatura é feito via transferência manual
- Não existe conceito de "fatura", "fechamento" ou "limite"

---

## 5. Arquivos Impactados por uma Evolução

Para implementar o tratamento correto de cartão de crédito, os seguintes arquivos/camadas precisariam ser modificados:

### Backend

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Domain | `Account.cs` | Adicionar `CreditLimit`, `ClosingDay`, `DueDay`, `DebitAccountId`. Alterar `ApplyDebit` para validar limite. Factory method específico para cartão |
| Domain | `AccountType.cs` | (Sem mudança — `Cartao=2` já existe) |
| Application | `CreateAccountCommand.cs` | Adicionar campos opcionais para cartão |
| Application | `CreateAccountValidator.cs` | Validação condicional: se tipo=Cartão → exigir limite, fechamento, vencimento |
| Application | `AccountResponse.cs` | Adicionar campos nullable para cartão |
| Application | `CreateAccountCommandHandler.cs` | Passar novos campos ao `Account.Create` |
| Infra | `AccountConfiguration.cs` | Mapear novas colunas |
| Infra | Migration nova | `ALTER TABLE accounts ADD COLUMN credit_limit, closing_day, due_day, debit_account_id` |
| API | `AccountsController.cs` | (Request/response já evoluem com os DTOs) |

### Frontend

| Arquivo | Mudança |
|---------|---------|
| `types/account.ts` | Adicionar campos no `CreateAccountRequest` e `AccountResponse` |
| `schemas/accountSchema.ts` | Validação condicional por tipo |
| `AccountForm.tsx` | Formulário dinâmico: se tipo=Cartão → mostrar campos de limite, fechamento, vencimento, conta de débito; ocultar saldo inicial e toggle de saldo negativo |
| `AccountCard.tsx` | Exibir "Limite: R$ X" e "Disponível: R$ Y" ao invés de "Saldo Atual" para cartão |
| `constants.ts` | Possível adição de labels específicos |

### Testes

| Arquivo | Mudança |
|---------|---------|
| `AccountTests.cs` | Testes para criação de cartão com limite, validação de limite disponível |
| `TransactionDomainServiceTests.cs` | Testes para débito em cartão respeitando limite |
| `AccountCommandHandlerTests.cs` | Testes para criação com campos de cartão |
| `AccountForm.test.tsx` | Testes para formulário dinâmico |
| `AccountCard.test.tsx` | Testes para exibição de limite/disponível |

---

## 6. Proposta de Modelo Evoluído

### 6.1 Entidade `Account` (com suporte a Cartão de Crédito)

```csharp
public class Account : BaseEntity
{
    // Campos comuns (todos os tipos)
    public string Name { get; private set; }
    public AccountType Type { get; private set; }
    public decimal Balance { get; private set; }
    public bool AllowNegativeBalance { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Campos específicos de Cartão de Crédito (nullable para outros tipos)
    public decimal? CreditLimit { get; private set; }
    public int? ClosingDay { get; private set; }        // 1-31
    public int? DueDay { get; private set; }             // 1-31
    public Guid? DebitAccountId { get; private set; }    // FK → accounts.id

    // Propriedade calculada
    public decimal? AvailableLimit => Type == AccountType.Cartao && CreditLimit.HasValue
        ? CreditLimit.Value + Balance  // Balance é negativo (fatura), então Available = Limite - |Balance|
        : null;

    // Factory para Cartão de Crédito
    public static Account CreateCreditCard(
        string name, decimal creditLimit, int closingDay, int dueDay,
        Guid? debitAccountId, string userId)
    {
        var account = new Account
        {
            Name = name,
            Type = AccountType.Cartao,
            Balance = 0m,                    // Cartão sempre começa com saldo 0
            AllowNegativeBalance = true,     // Cartão sempre permite saldo negativo
            CreditLimit = creditLimit,
            ClosingDay = closingDay,
            DueDay = dueDay,
            DebitAccountId = debitAccountId,
        };
        account.SetAuditOnCreate(userId);
        return account;
    }

    // Sobrecarga de ApplyDebit para cartão
    public void ApplyDebit(decimal amount, string userId)
    {
        if (Type == AccountType.Cartao)
        {
            // Para cartão: valida se a compra excede o limite
            if (CreditLimit.HasValue && Math.Abs(Balance - amount) > CreditLimit.Value)
                throw new CreditLimitExceededException(Id, CreditLimit.Value, Balance, amount);
        }
        else
        {
            if (!AllowNegativeBalance && Balance - amount < 0)
                throw new InsufficientBalanceException(Id, Balance, amount);
        }

        Balance -= amount;
        SetAuditOnUpdate(userId);
    }
}
```

### 6.2 Tabela `accounts` (colunas adicionais)

```sql
ALTER TABLE accounts ADD COLUMN credit_limit numeric(18,2) NULL;
ALTER TABLE accounts ADD COLUMN closing_day smallint NULL;
ALTER TABLE accounts ADD COLUMN due_day smallint NULL;
ALTER TABLE accounts ADD COLUMN debit_account_id uuid NULL
    REFERENCES accounts(id) ON DELETE SET NULL;
```

### 6.3 Formulário Frontend (Comportamento Dinâmico)

```
Se tipo == Corrente | Investimento | Carteira:
    ├── Nome da Conta
    ├── Tipo de Conta
    ├── Saldo Inicial
    └── Permitir Saldo Negativo

Se tipo == Cartão:
    ├── Nome da Conta
    ├── Tipo de Conta (= Cartão, fixo)
    ├── Limite de Crédito          ← substitui "Saldo Inicial"
    ├── Dia de Fechamento          ← novo
    ├── Dia de Vencimento          ← novo
    └── Conta de Débito (select)   ← novo (lista contas Corrente e Carteira)
```

---

## 7. Resumo

| Aspecto | Estado Atual | Problema |
|---------|--------------|----------|
| **Modelo de dados** | Genérico, sem campos de cartão | Não modela limite, fechamento, vencimento |
| **Formulário** | Idêntico para todos os tipos | Pede "saldo inicial" para cartão (deveria ser "limite") |
| **Validação** | Mesma para todos os tipos | Falta validação condicional por tipo |
| **Exibição** | "Saldo Atual" para todos | Cartão deveria mostrar limite e disponível |
| **Comportamento de débito** | Valida `AllowNegativeBalance` | Cartão deveria validar contra limite de crédito |
| **PRD** | Adiou para v1.2+ | Decisão consciente — pronto para evolução |

A decisão de tratar o cartão como conta comum foi **intencional no MVP**. Para evoluir, o impacto é moderado (adição de campos nullable na mesma tabela, formulário dinâmico no frontend, validação condicional) e não quebra a modelagem existente.
