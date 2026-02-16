using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class CreditCardDetails
{
    public decimal CreditLimit { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public Guid DebitAccountId { get; private set; }
    public bool EnforceCreditLimit { get; private set; }

    protected CreditCardDetails() { }

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
        if (debitAccountId == Guid.Empty)
            throw new InvalidCreditCardConfigException("Conta de débito é obrigatória.");

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
        if (debitAccountId == Guid.Empty)
            throw new InvalidCreditCardConfigException("Conta de débito é obrigatória.");

        CreditLimit = creditLimit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        DebitAccountId = debitAccountId;
        EnforceCreditLimit = enforceCreditLimit;
    }
}
