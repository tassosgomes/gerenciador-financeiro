using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Service;

namespace GestorFinanceiro.Financeiro.UnitTests;

public class TransactionDomainServiceTests
{
    private readonly TransactionDomainService _sut = new();

    [Fact]
    public void CreateTransaction_StatusPaid_AplicaSaldoNaConta()
    {
        var account = CreateActiveAccount(initialBalance: 100m);

        var transaction = _sut.CreateTransaction(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            40m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        transaction.Status.Should().Be(TransactionStatus.Paid);
        account.Balance.Should().Be(60m);
    }

    [Fact]
    public void CreateTransaction_StatusPending_NaoAplicaSaldo()
    {
        var account = CreateActiveAccount(initialBalance: 100m);

        var transaction = _sut.CreateTransaction(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            40m,
            "Despesa",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(10),
            TransactionStatus.Pending,
            "user-1");

        transaction.Status.Should().Be(TransactionStatus.Pending);
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void CreateTransaction_ContaInativa_LancaInactiveAccountException()
    {
        var account = CreateActiveAccount();
        account.Deactivate("user-2");

        var action = () => _sut.CreateTransaction(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            10m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        action.Should().Throw<InactiveAccountException>();
    }

    [Fact]
    public void CreateAdjustment_DebitOriginalValorMaior_CriaDebitComDiferenca()
    {
        var account = CreateActiveAccount(initialBalance: 200m);
        var original = CreateTransaction(TransactionType.Debit, 100m, TransactionStatus.Paid);

        var adjustment = _sut.CreateAdjustment(account, original, 130m, "user-1");

        adjustment.Type.Should().Be(TransactionType.Debit);
        adjustment.Amount.Should().Be(30m);
        account.Balance.Should().Be(170m);
    }

    [Fact]
    public void CreateAdjustment_DebitOriginalValorMenor_CriaCreditComDiferenca()
    {
        var account = CreateActiveAccount(initialBalance: 200m);
        var original = CreateTransaction(TransactionType.Debit, 100m, TransactionStatus.Paid);

        var adjustment = _sut.CreateAdjustment(account, original, 80m, "user-1");

        adjustment.Type.Should().Be(TransactionType.Credit);
        adjustment.Amount.Should().Be(20m);
        account.Balance.Should().Be(220m);
    }

    [Fact]
    public void CreateAdjustment_CreditOriginalValorMaior_CriaCreditComDiferenca()
    {
        var account = CreateActiveAccount(initialBalance: 200m);
        var original = CreateTransaction(TransactionType.Credit, 100m, TransactionStatus.Paid);

        var adjustment = _sut.CreateAdjustment(account, original, 140m, "user-1");

        adjustment.Type.Should().Be(TransactionType.Credit);
        adjustment.Amount.Should().Be(40m);
        account.Balance.Should().Be(240m);
    }

    [Fact]
    public void CreateAdjustment_CreditOriginalValorMenor_CriaDebitComDiferenca()
    {
        var account = CreateActiveAccount(initialBalance: 200m);
        var original = CreateTransaction(TransactionType.Credit, 100m, TransactionStatus.Paid);

        var adjustment = _sut.CreateAdjustment(account, original, 70m, "user-1");

        adjustment.Type.Should().Be(TransactionType.Debit);
        adjustment.Amount.Should().Be(30m);
        account.Balance.Should().Be(170m);
    }

    [Fact]
    public void CreateAdjustment_ValorIgual_LancaExcecao()
    {
        var account = CreateActiveAccount(initialBalance: 200m);
        var original = CreateTransaction(TransactionType.Debit, 100m, TransactionStatus.Paid);

        var action = () => _sut.CreateAdjustment(account, original, 100m, "user-1");

        action.Should().Throw<AdjustmentAmountUnchangedException>();
    }

    [Fact]
    public void CreateAdjustment_MarcaOriginalComoAjustada()
    {
        var account = CreateActiveAccount(initialBalance: 200m);
        var original = CreateTransaction(TransactionType.Debit, 100m, TransactionStatus.Paid);

        _sut.CreateAdjustment(account, original, 120m, "user-1");

        original.HasAdjustment.Should().BeTrue();
    }

    [Fact]
    public void CancelTransaction_StatusPaid_ReverteSaldo()
    {
        var account = CreateActiveAccount(initialBalance: 100m);
        var transaction = _sut.CreateTransaction(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            30m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        _sut.CancelTransaction(account, transaction, "user-2", "Cancelada");

        transaction.Status.Should().Be(TransactionStatus.Cancelled);
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void CancelTransaction_StatusPending_NaoReverteSaldo()
    {
        var account = CreateActiveAccount(initialBalance: 100m);
        var transaction = _sut.CreateTransaction(
            account,
            Guid.NewGuid(),
            TransactionType.Debit,
            30m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Pending,
            "user-1");

        _sut.CancelTransaction(account, transaction, "user-2", "Cancelada");

        transaction.Status.Should().Be(TransactionStatus.Cancelled);
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void CreateTransaction_DebitOnCreditCardWithEnforceLimit_ExceedsLimit_ShouldThrowCreditLimitExceededException()
    {
        // Arrange
        var creditCard = Account.CreateCreditCard(
            "Cartão Visa",
            creditLimit: 1000m,
            closingDay: 10,
            dueDay: 20,
            debitAccountId: Guid.NewGuid(),
            enforceCreditLimit: true,
            userId: "user-1");

        // Act & Assert
        var action = () => _sut.CreateTransaction(
            creditCard,
            Guid.NewGuid(),
            TransactionType.Debit,
            1500m,
            "Compra acima do limite",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        action.Should().Throw<CreditLimitExceededException>();
    }

    [Fact]
    public void CreateTransaction_DebitOnCreditCardWithEnforceLimit_WithinLimit_ShouldSucceed()
    {
        // Arrange
        var creditCard = Account.CreateCreditCard(
            "Cartão Visa",
            creditLimit: 1000m,
            closingDay: 10,
            dueDay: 20,
            debitAccountId: Guid.NewGuid(),
            enforceCreditLimit: true,
            userId: "user-1");

        // Act
        var transaction = _sut.CreateTransaction(
            creditCard,
            Guid.NewGuid(),
            TransactionType.Debit,
            500m,
            "Compra dentro do limite",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        // Assert
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(500m);
        creditCard.Balance.Should().Be(-500m);
        creditCard.GetAvailableLimit().Should().Be(500m);
    }

    [Fact]
    public void CreateTransaction_DebitOnCreditCardWithoutEnforceLimit_ExceedsLimit_ShouldSucceed()
    {
        // Arrange
        var creditCard = Account.CreateCreditCard(
            "Cartão Mastercard",
            creditLimit: 1000m,
            closingDay: 10,
            dueDay: 20,
            debitAccountId: Guid.NewGuid(),
            enforceCreditLimit: false,
            userId: "user-1");

        // Act
        var transaction = _sut.CreateTransaction(
            creditCard,
            Guid.NewGuid(),
            TransactionType.Debit,
            1500m,
            "Compra acima do limite informativo",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        // Assert
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(1500m);
        creditCard.Balance.Should().Be(-1500m);
    }

    [Fact]
    public void CreateTransaction_DebitOnRegularAccount_ShouldNotCallValidateCreditLimit()
    {
        // Arrange
        var regularAccount = CreateActiveAccount(initialBalance: 100m);

        // Act
        var transaction = _sut.CreateTransaction(
            regularAccount,
            Guid.NewGuid(),
            TransactionType.Debit,
            50m,
            "Despesa normal",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        // Assert
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(50m);
        regularAccount.Balance.Should().Be(50m);
    }

    [Fact]
    public void CreateTransaction_CreditOnCreditCard_ShouldNotValidateLimit()
    {
        // Arrange
        var creditCard = Account.CreateCreditCard(
            "Cartão Visa",
            creditLimit: 1000m,
            closingDay: 10,
            dueDay: 20,
            debitAccountId: Guid.NewGuid(),
            enforceCreditLimit: true,
            userId: "user-1");

        var debitTransaction = _sut.CreateTransaction(
            creditCard,
            Guid.NewGuid(),
            TransactionType.Debit,
            800m,
            "Compra",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        // Act - Pagamento/crédito nunca é bloqueado, mesmo que exceda o limite
        var creditTransaction = _sut.CreateTransaction(
            creditCard,
            Guid.NewGuid(),
            TransactionType.Credit,
            1500m,
            "Pagamento de fatura",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        // Assert
        creditTransaction.Should().NotBeNull();
        creditTransaction.Amount.Should().Be(1500m);
        creditCard.Balance.Should().Be(700m);
    }

    private static Account CreateActiveAccount(decimal initialBalance = 100m)
    {
        return Account.Create("Conta", AccountType.Corrente, initialBalance, false, "user-1");
    }

    private static Transaction CreateTransaction(TransactionType type, decimal amount, TransactionStatus status)
    {
        return Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            amount,
            "Transacao",
            DateTime.UtcNow,
            null,
            status,
            "user-1");
    }
}
