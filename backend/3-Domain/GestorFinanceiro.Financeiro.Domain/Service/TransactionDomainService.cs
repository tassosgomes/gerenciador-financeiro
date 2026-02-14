using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.Domain.Service;

public class TransactionDomainService
{
    public Transaction CreateTransaction(
        Account account,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        string description,
        DateTime competenceDate,
        DateTime? dueDate,
        TransactionStatus status,
        string userId,
        string? operationId = null)
    {
        account.ValidateCanReceiveTransaction();

        var transaction = Transaction.Create(
            account.Id,
            categoryId,
            type,
            amount,
            description,
            competenceDate,
            dueDate,
            status,
            userId,
            operationId);

        if (status == TransactionStatus.Paid)
        {
            ApplyBalanceImpact(account, type, amount, userId);
        }

        return transaction;
    }

    public Transaction CreateAdjustment(
        Account account,
        Transaction original,
        decimal correctAmount,
        string userId,
        string? operationId = null)
    {
        var difference = correctAmount - original.Amount;
        if (difference == 0)
        {
            throw new AdjustmentAmountUnchangedException();
        }

        TransactionType adjustmentType;
        decimal absDifference;

        if (original.Type == TransactionType.Debit)
        {
            adjustmentType = difference > 0 ? TransactionType.Debit : TransactionType.Credit;
            absDifference = Math.Abs(difference);
        }
        else
        {
            adjustmentType = difference > 0 ? TransactionType.Credit : TransactionType.Debit;
            absDifference = Math.Abs(difference);
        }

        var adjustment = Transaction.CreateAdjustment(
            account.Id,
            original.CategoryId,
            adjustmentType,
            absDifference,
            original.Id,
            $"Ajuste ref. transação {original.Id}",
            original.CompetenceDate,
            userId,
            operationId);

        ApplyBalanceImpact(account, adjustmentType, absDifference, userId);
        original.MarkAsAdjusted(userId);

        return adjustment;
    }

    public void CancelTransaction(
        Account account,
        Transaction transaction,
        string userId,
        string? reason = null)
    {
        var previousStatus = transaction.Status;
        transaction.Cancel(userId, reason);

        if (previousStatus == TransactionStatus.Paid)
        {
            RevertBalanceImpact(account, transaction.Type, transaction.Amount, userId);
        }
    }

    private static void ApplyBalanceImpact(
        Account account,
        TransactionType type,
        decimal amount,
        string userId)
    {
        if (type == TransactionType.Debit)
        {
            account.ApplyDebit(amount, userId);
            return;
        }

        account.ApplyCredit(amount, userId);
    }

    private static void RevertBalanceImpact(
        Account account,
        TransactionType type,
        decimal amount,
        string userId)
    {
        if (type == TransactionType.Debit)
        {
            account.RevertDebit(amount, userId);
            return;
        }

        account.RevertCredit(amount, userId);
    }
}
