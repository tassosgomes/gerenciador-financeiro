using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.Domain.Service;

public class InstallmentDomainService
{
    private readonly TransactionDomainService _transactionService;

    public InstallmentDomainService(TransactionDomainService transactionService)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
    }

    public IReadOnlyList<Transaction> CreateInstallmentGroup(
        Account account,
        Guid categoryId,
        TransactionType type,
        decimal totalAmount,
        int installmentCount,
        string description,
        DateTime firstCompetenceDate,
        DateTime firstDueDate,
        string userId,
        string? operationId = null)
    {
        account.ValidateCanReceiveTransaction();

        var groupId = Guid.NewGuid();
        var installmentAmount = Math.Round(totalAmount / installmentCount, 2);
        var remainder = totalAmount - (installmentAmount * installmentCount);
        var transactions = new List<Transaction>();

        for (var i = 0; i < installmentCount; i++)
        {
            var amount = installmentAmount;
            if (i == installmentCount - 1)
            {
                amount += remainder;
            }

            var competenceDate = firstCompetenceDate.AddMonths(i);
            var dueDate = firstDueDate.AddMonths(i);

            var transaction = _transactionService.CreateTransaction(
                account,
                categoryId,
                type,
                amount,
                $"{description} ({i + 1}/{installmentCount})",
                competenceDate,
                dueDate,
                TransactionStatus.Pending,
                userId,
                i == 0 ? operationId : null);

            transaction.SetInstallmentInfo(groupId, i + 1, installmentCount);
            transactions.Add(transaction);
        }

        return transactions.AsReadOnly();
    }

    public IReadOnlyList<Transaction> AdjustInstallmentGroup(
        Account account,
        IEnumerable<Transaction> groupTransactions,
        decimal newTotalAmount,
        string userId,
        string? operationId = null)
    {
        var transactions = groupTransactions.ToList();
        var pending = transactions
            .Where(transaction => transaction.Status == TransactionStatus.Pending)
            .OrderBy(transaction => transaction.InstallmentNumber)
            .ToList();

        if (pending.Count == 0)
        {
            throw new NoPendingInstallmentsToAdjustException();
        }

        var paidTotal = transactions
            .Where(transaction => transaction.Status == TransactionStatus.Paid)
            .Sum(transaction => transaction.Amount);

        var remainingAmount = newTotalAmount - paidTotal;
        var perInstallment = Math.Round(remainingAmount / pending.Count, 2);
        var remainder = remainingAmount - (perInstallment * pending.Count);
        var adjustments = new List<Transaction>();

        for (var i = 0; i < pending.Count; i++)
        {
            var target = pending[i];
            var correctAmount = perInstallment;
            if (i == pending.Count - 1)
            {
                correctAmount += remainder;
            }

            if (correctAmount == target.Amount)
            {
                continue;
            }

            var adjustment = _transactionService.CreateAdjustment(
                account,
                target,
                correctAmount,
                userId,
                i == 0 ? operationId : null);

            adjustments.Add(adjustment);
        }

        return adjustments.AsReadOnly();
    }

    public void CancelSingleInstallment(
        Account account,
        Transaction installment,
        string userId,
        string? reason = null)
    {
        if (installment.Status == TransactionStatus.Paid)
        {
            throw new InstallmentPaidCannotBeCancelledException(installment.Id);
        }

        _transactionService.CancelTransaction(account, installment, userId, reason);
    }

    public void CancelInstallmentGroup(
        Account account,
        IEnumerable<Transaction> groupTransactions,
        string userId,
        string? reason = null)
    {
        foreach (var transaction in groupTransactions.Where(transaction => transaction.Status == TransactionStatus.Pending))
        {
            _transactionService.CancelTransaction(account, transaction, userId, reason);
        }
    }
}
