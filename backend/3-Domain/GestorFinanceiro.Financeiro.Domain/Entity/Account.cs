using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class Account : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public decimal Balance { get; private set; }
    public bool AllowNegativeBalance { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Account Create(
        string name,
        AccountType type,
        decimal initialBalance,
        bool allowNegativeBalance,
        string userId)
    {
        var account = new Account
        {
            Name = name,
            Type = type,
            Balance = initialBalance,
            AllowNegativeBalance = allowNegativeBalance,
        };

        account.SetAuditOnCreate(userId);
        return account;
    }

    public void Activate(string userId)
    {
        IsActive = true;
        SetAuditOnUpdate(userId);
    }

    public void Deactivate(string userId)
    {
        IsActive = false;
        SetAuditOnUpdate(userId);
    }

    public void Update(string name, bool allowNegativeBalance, string userId)
    {
        Name = name;
        AllowNegativeBalance = allowNegativeBalance;
        SetAuditOnUpdate(userId);
    }

    public void ApplyDebit(decimal amount, string userId)
    {
        if (!AllowNegativeBalance && Balance - amount < 0)
        {
            throw new InsufficientBalanceException(Id, Balance, amount);
        }

        Balance -= amount;
        SetAuditOnUpdate(userId);
    }

    public void ApplyCredit(decimal amount, string userId)
    {
        Balance += amount;
        SetAuditOnUpdate(userId);
    }

    public void RevertDebit(decimal amount, string userId)
    {
        Balance += amount;
        SetAuditOnUpdate(userId);
    }

    public void RevertCredit(decimal amount, string userId)
    {
        Balance -= amount;
        SetAuditOnUpdate(userId);
    }

    public void ValidateCanReceiveTransaction()
    {
        if (!IsActive)
        {
            throw new InactiveAccountException(Id);
        }
    }
}
