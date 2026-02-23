namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class Establishment : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Cnpj { get; private set; } = string.Empty;
    public string AccessKey { get; private set; } = string.Empty;

    public Transaction Transaction { get; private set; } = null!;

    public static Establishment Create(
        Guid transactionId,
        string name,
        string cnpj,
        string accessKey,
        string userId)
    {
        var establishment = new Establishment
        {
            TransactionId = transactionId,
            Name = name,
            Cnpj = cnpj,
            AccessKey = accessKey,
        };

        establishment.SetAuditOnCreate(userId);
        return establishment;
    }
}
