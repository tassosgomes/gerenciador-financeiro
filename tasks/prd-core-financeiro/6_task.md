---
status: pending
parallelizable: true
blocked_by: ["5.0"]
---

<task_context>
<domain>engine/testes</domain>
<type>testing</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>nenhuma</dependencies>
<unblocks></unblocks>
</task_context>

# Tarefa 6.0: Testes Unitários do Domain

## Visão Geral

Criar os testes unitários para toda a camada de domínio: entidades e domain services. Os testes de entidades são de lógica pura (sem mocks), enquanto os testes de domain services testam a orquestração de regras de negócio entre entidades (sem mocks de repositórios, pois domain services não os usam diretamente).

O objetivo é atingir cobertura ≥ 90% conforme definido no PRD e na techspec.

## Requisitos

- PRD: cobertura ≥ 90% no domínio
- Techspec: framework xUnit + AwesomeAssertions + Moq + AutoFixture
- Techspec: naming convention `MetodoTestado_Cenario_ResultadoEsperado`
- `rules/dotnet-testing.md`: padrão AAA (Arrange, Act, Assert)

## Subtarefas

### Testes de Entidades (lógica pura — sem mocks)

- [ ] 6.1 Testes para `Account`:
  - `Create_DadosValidos_CriaContaComSaldoInicial`
  - `ApplyDebit_SaldoSuficiente_DiminuiSaldo`
  - `ApplyDebit_SaldoInsuficienteSemPermissao_LancaInsufficientBalanceException`
  - `ApplyDebit_SaldoInsuficienteComPermissao_PermiteDebito`
  - `ApplyCredit_ValorPositivo_AumentaSaldo`
  - `RevertDebit_ValorDebito_AumentaSaldo`
  - `RevertCredit_ValorCredito_DiminuiSaldo`
  - `Activate_ContaInativa_AtivaENaoLancaExcecao`
  - `Deactivate_ContaAtiva_DesativaComSucesso`
  - `ValidateCanReceiveTransaction_ContaInativa_LancaInactiveAccountException`
  - `ValidateCanReceiveTransaction_ContaAtiva_NaoLancaExcecao`

- [ ] 6.2 Testes para `Transaction`:
  - `Create_ValorPositivo_CriaTransacaoComStatusCorreto`
  - `Create_ValorZero_LancaInvalidTransactionAmountException`
  - `Create_ValorNegativo_LancaInvalidTransactionAmountException`
  - `Cancel_TransacaoPending_AlteraStatusParaCancelled`
  - `Cancel_TransacaoJaCancelada_LancaTransactionAlreadyCancelledException`
  - `MarkAsAdjusted_TransacaoNormal_DefineHasAdjustmentTrue`
  - `CreateAdjustment_DiferencaPositiva_CriaAjusteVinculado`
  - `IsOverdue_PendingComDueDatePassada_RetornaTrue`
  - `IsOverdue_PendingComDueDateFutura_RetornaFalse`
  - `IsOverdue_PaidComDueDatePassada_RetornaFalse`
  - `IsOverdue_PendingSemDueDate_RetornaFalse`
  - `SetInstallmentInfo_DadosValidos_DefineGrupoENumero`
  - `SetRecurrenceInfo_TemplateId_DefineFlagETemplate`
  - `SetTransferGroup_GroupId_DefineTransferGroupId`

- [ ] 6.3 Testes para `Category`:
  - `Create_DadosValidos_CriaCategoriaComTipo`
  - `UpdateName_NovoNome_AtualizaNomeComAuditoria`

- [ ] 6.4 Testes para `RecurrenceTemplate`:
  - `Create_DadosValidos_CriaTemplateAtivo`
  - `ShouldGenerateForMonth_SemLastGenerated_RetornaTrue`
  - `ShouldGenerateForMonth_MesMaiorQueUltimoGerado_RetornaTrue`
  - `ShouldGenerateForMonth_MesIgualUltimoGerado_RetornaFalse`
  - `ShouldGenerateForMonth_TemplateInativo_RetornaFalse`
  - `Deactivate_TemplateAtivo_DesativaComSucesso`
  - `MarkGenerated_Data_AtualizaLastGeneratedDate`

### Testes de Domain Services

- [ ] 6.5 Testes para `TransactionDomainService`:
  - `CreateTransaction_StatusPaid_AplicaSaldoNaConta`
  - `CreateTransaction_StatusPending_NaoAplicaSaldo`
  - `CreateTransaction_ContaInativa_LancaInactiveAccountException`
  - `CreateAdjustment_DebitOriginalValorMaior_CriaDebitComDiferenca`
  - `CreateAdjustment_DebitOriginalValorMenor_CriaCreditComDiferenca`
  - `CreateAdjustment_CreditOriginalValorMaior_CriaCreditComDiferenca`
  - `CreateAdjustment_CreditOriginalValorMenor_CriaDebitComDiferenca`
  - `CreateAdjustment_ValorIgual_LancaExcecao`
  - `CreateAdjustment_MarcaOriginalComoAjustada`
  - `CancelTransaction_StatusPaid_ReverteSaldo`
  - `CancelTransaction_StatusPending_NaoReverteSaldo`

- [ ] 6.6 Testes para `InstallmentDomainService`:
  - `CreateInstallmentGroup_ValorDivisivel_CriaParcelasIguais`
  - `CreateInstallmentGroup_ValorComResiduo_AplicaResiduoNaUltimaParcela`
  - `CreateInstallmentGroup_ParcelasTemInfoCorreta_GrupoNumerosTotal`
  - `AdjustInstallmentGroup_ParcelasPending_RedistribuiDiferenca`
  - `AdjustInstallmentGroup_SemPending_LancaExcecao`
  - `CancelSingleInstallment_ParcelaPending_CancelaComSucesso`
  - `CancelSingleInstallment_ParcelaPaid_LancaInstallmentPaidCannotBeCancelledException`
  - `CancelInstallmentGroup_MixPaidPending_CancelaSomentePending`

- [ ] 6.7 Testes para `TransferDomainService`:
  - `CreateTransfer_ContasAtivas_CriaDebitECreditComTransferGroup`
  - `CreateTransfer_ContaOrigem_SaldoReduzido`
  - `CreateTransfer_ContaDestino_SaldoAumentado`
  - `CancelTransfer_TransferenciaExistente_ReverteSaldosEmAmbasContas`

- [ ] 6.8 Testes para `RecurrenceDomainService`:
  - `GenerateNextOccurrence_MesNaoGerado_CriaTransacaoRecorrente`
  - `GenerateNextOccurrence_MesJaGerado_RetornaNull`
  - `GenerateNextOccurrence_TemplateInativo_RetornaNull`
  - `GenerateNextOccurrence_Dia31EmMesCom28Dias_NormalizaDia`

## Sequenciamento

- Bloqueado por: 5.0 (domain services devem estar implementados)
- Desbloqueia: Nenhuma dependência direta, mas garante confiança para avançar
- Paralelizável: Sim — pode ser executada em paralelo com 7.0 e 8.0

## Detalhes de Implementação

### Localização dos testes

```
5-Tests/GestorFinanceiro.Financeiro.UnitTests/
├── Entity/
│   ├── AccountTests.cs
│   ├── TransactionTests.cs
│   ├── CategoryTests.cs
│   └── RecurrenceTemplateTests.cs
└── Service/
    ├── TransactionDomainServiceTests.cs
    ├── InstallmentDomainServiceTests.cs
    ├── TransferDomainServiceTests.cs
    └── RecurrenceDomainServiceTests.cs
```

### Padrão de teste (AAA)

```csharp
[Fact]
public void ApplyDebit_SaldoInsuficienteSemPermissao_LancaInsufficientBalanceException()
{
    // Arrange
    var account = Account.Create("Conta Teste", AccountType.Corrente, 100m, false, "user1");

    // Act
    var act = () => account.ApplyDebit(150m, "user1");

    // Assert
    act.Should().Throw<InsufficientBalanceException>();
}
```

### Observações

- Testes de entidades são lógica pura — sem mocks nem banco de dados
- Testes de domain services também são lógica pura — os services operam diretamente nas entidades (não usam repositórios)
- Usar `AwesomeAssertions` (`.Should().Be...`, `.Should().Throw...`)
- Usar `AutoFixture` para gerar dados de teste repetitivos quando benéfico
- Para validar arredondamento de parcelas, testar com valores que geram resíduo (ex: R$ 100,00 ÷ 3 = R$ 33,33 + R$ 33,33 + R$ 33,34)

## Critérios de Sucesso

- Todos os testes passam com `dotnet test`
- Cobertura ≥ 90% na camada Domain (entidades + services)
- Todos os cenários críticos da techspec estão cobertos (saldo negativo, parcela paga, arredondamento, overdue, recorrência duplicada)
- Naming convention `MetodoTestado_Cenario_ResultadoEsperado` seguida em 100% dos testes
- Padrão AAA seguido em todos os testes
