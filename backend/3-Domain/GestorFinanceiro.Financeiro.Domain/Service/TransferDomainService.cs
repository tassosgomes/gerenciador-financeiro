using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Service;

public class TransferDomainService
{
    private readonly TransactionDomainService _transactionService;

    public TransferDomainService(TransactionDomainService transactionService)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
    }

    public (Transaction debit, Transaction credit) CreateTransfer(
        Account sourceAccount,
        Account destinationAccount,
        Guid categoryId,
        decimal amount,
        string description,
        DateTime competenceDate,
        string userId,
        string? operationId = null)
    {
        var transferGroupId = Guid.NewGuid();

        var debit = _transactionService.CreateTransaction(
            sourceAccount,
            categoryId,
            TransactionType.Debit,
            amount,
            $"Transf. para {destinationAccount.Name}: {description}",
            competenceDate,
            null,
            TransactionStatus.Paid,
            userId,
            operationId);

        debit.SetTransferGroup(transferGroupId);

        var credit = _transactionService.CreateTransaction(
            destinationAccount,
            categoryId,
            TransactionType.Credit,
            amount,
            $"Transf. de {sourceAccount.Name}: {description}",
            competenceDate,
            null,
            TransactionStatus.Paid,
            userId);

        credit.SetTransferGroup(transferGroupId);

        return (debit, credit);
    }

    public void CancelTransfer(
        Account sourceAccount,
        Account destinationAccount,
        Transaction debit,
        Transaction credit,
        string userId,
        string? reason = null)
    {
        _transactionService.CancelTransaction(sourceAccount, debit, userId, reason);
        _transactionService.CancelTransaction(destinationAccount, credit, userId, reason);
    }

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

        var debitTransaction = _transactionService.CreateTransaction(
            debitAccount,
            categoryId,
            TransactionType.Debit,
            amount,
            description,
            competenceDate,
            null,
            TransactionStatus.Paid,
            userId,
            operationId);

        debitTransaction.SetTransferGroup(transferGroupId);

        var creditTransaction = _transactionService.CreateTransaction(
            creditCardAccount,
            categoryId,
            TransactionType.Credit,
            amount,
            description,
            competenceDate,
            null,
            TransactionStatus.Paid,
            userId,
            operationId != null ? $"{operationId}-credit" : null);

        creditTransaction.SetTransferGroup(transferGroupId);

        return new List<Transaction> { debitTransaction, creditTransaction };
    }
}
