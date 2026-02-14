---
status: pending
parallelizable: true
blocked_by: ["3.0", "4.0"]
---

<task_context>
<domain>engine/domínio</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>nenhuma</dependencies>
<unblocks>"6.0", "9.0"</unblocks>
</task_context>

# Tarefa 5.0: Domain Layer — Domain Services

## Visão Geral

Implementar os domain services que orquestram regras de negócio entre entidades. São 4 serviços: `TransactionDomainService` (criação, ajuste, cancelamento), `InstallmentDomainService` (parcelamento), `TransferDomainService` (transferências) e `RecurrenceDomainService` (recorrência lazy).

Os domain services encapsulam operações que envolvem múltiplas entidades ou regras complexas que não pertencem a uma única entidade. Eles NÃO acessam repositórios diretamente — recebem entidades já carregadas e operam sobre elas.

## Requisitos

- PRD F3 req 13–19: criação de transação com impacto no saldo
- PRD F4 req 20–23: ajuste por diferença contábil — nunca altera a transação original
- PRD F5 req 24–27: cancelamento lógico com reversão de saldo se era `Paid`
- PRD F6 req 28–32: parcelamento com arredondamento financeiro e resíduo na última parcela
- PRD F7 req 32–35: recorrência lazy — gera apenas 1 mês à frente
- PRD F8 req 36–39: transferência gera Debit + Credit vinculados por `TransferGroup`
- Decisão 5 do PRD: ajuste em grupo de parcelas — divisão igualitária com arredondamento

## Subtarefas

- [ ] 5.1 Criar `TransactionDomainService` com métodos: `CreateTransaction`, `CreateAdjustment`, `CancelTransaction` (e métodos privados `ApplyBalanceImpact`, `RevertBalanceImpact`)
- [ ] 5.2 Criar `InstallmentDomainService` com métodos: `CreateInstallmentGroup`, `AdjustInstallmentGroup`, `CancelSingleInstallment`, `CancelInstallmentGroup`
- [ ] 5.3 Criar `TransferDomainService` com métodos: `CreateTransfer`, `CancelTransfer`
- [ ] 5.4 Criar `RecurrenceDomainService` com método: `GenerateNextOccurrence`

## Sequenciamento

- Bloqueado por: 3.0 (entidades), 4.0 (interfaces — embora domain services não usem repositórios diretamente, os testes vão querer mock-ar)
- Desbloqueia: 6.0 (testes unitários do domínio), 9.0 (Application Layer usa domain services)
- Paralelizável: Sim — pode ser executada em paralelo com 7.0 (Infra Layer — DbContext)

## Detalhes de Implementação

### Localização dos arquivos

```
3-Domain/GestorFinanceiro.Financeiro.Domain/
└── Service/
    ├── TransactionDomainService.cs
    ├── InstallmentDomainService.cs
    ├── TransferDomainService.cs
    └── RecurrenceDomainService.cs
```

### TransactionDomainService

```csharp
public class TransactionDomainService
{
    // CreateTransaction: cria transação, aplica saldo se Paid
    // CreateAdjustment: calcula diferença, cria transação compensatória, marca original como ajustada
    // CancelTransaction: cancela logicamente, reverte saldo se era Paid
    // ApplyBalanceImpact (private): Debit → account.ApplyDebit, Credit → account.ApplyCredit
    // RevertBalanceImpact (private): Debit → account.RevertDebit, Credit → account.RevertCredit
}
```

**Regras do ajuste por diferença (F4):**
- Original `Debit 100`, correto `130` → ajuste `Debit 30` (mais débito)
- Original `Debit 100`, correto `80` → ajuste `Credit 20` (reversão parcial)
- Original `Credit 100`, correto `130` → ajuste `Credit 30`
- Original `Credit 100`, correto `80` → ajuste `Debit 20`
- Se diferença = 0, lançar exceção

### InstallmentDomainService

```csharp
public class InstallmentDomainService
{
    // CreateInstallmentGroup: divide valor total em N parcelas com arredondamento
    // AdjustInstallmentGroup: redistribui diferença entre parcelas Pending (Decisão 5 do PRD)
    // CancelSingleInstallment: cancela parcela individual (apenas se Pending — req 31)
    // CancelInstallmentGroup: cancela todas as Pending do grupo (req 32)
}
```

**Regras de arredondamento (Decisão 5 do PRD):**
- Dividir igualmente entre parcelas futuras (`Pending`)
- Arredondar para 2 casas decimais
- Resíduo aplicado na última parcela
- Exemplo: diferença +R$ 10,00 com 3 parcelas → R$ 3,33 + R$ 3,33 + R$ 3,34

### TransferDomainService

```csharp
public class TransferDomainService
{
    // CreateTransfer: cria Debit na origem + Credit no destino, compartilhando TransferGroupId
    // CancelTransfer: cancela ambas as transações, revertendo saldos
}
```

### RecurrenceDomainService

```csharp
public class RecurrenceDomainService
{
    // GenerateNextOccurrence: gera transação para o mês de referência se ShouldGenerateForMonth() = true
    // Normaliza dia do mês com Math.Min(dayOfMonth, DaysInMonth)
    // Define flag IsRecurrent e referência ao template
}
```

### Observações

- Domain services NÃO acessam repositórios — recebem entidades pré-carregadas
- Domain services NÃO gerenciam transações de banco — isso é responsabilidade dos Handlers (Application Layer)
- `InstallmentDomainService` depende de `TransactionDomainService` (composição)
- `TransferDomainService` depende de `TransactionDomainService` (composição)
- `RecurrenceDomainService` depende de `TransactionDomainService` (composição)

## Critérios de Sucesso

- Os 4 domain services compilam sem erros
- `CreateTransaction` cria transação e aplica saldo apenas quando status é `Paid`
- `CreateAdjustment` calcula a diferença corretamente para todos os cenários (Debit↑, Debit↓, Credit↑, Credit↓)
- `CancelTransaction` reverte saldo apenas quando transação era `Paid`
- `CreateInstallmentGroup` distribui valor corretamente com arredondamento e resíduo na última parcela
- `CancelSingleInstallment` rejeita parcela com status `Paid` via exceção
- `CreateTransfer` vincula as duas transações pelo `TransferGroupId`
- `GenerateNextOccurrence` retorna `null` quando o mês já foi gerado
- `dotnet build` compila sem erros
