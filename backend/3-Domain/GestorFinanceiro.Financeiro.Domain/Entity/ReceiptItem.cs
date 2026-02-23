namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class ReceiptItem : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ProductCode { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public int ItemOrder { get; private set; }

    public Transaction Transaction { get; private set; } = null!;

    public static ReceiptItem Create(
        Guid transactionId,
        string description,
        string? productCode,
        decimal quantity,
        string unitOfMeasure,
        decimal unitPrice,
        decimal totalPrice,
        int itemOrder,
        string userId)
    {
        var item = new ReceiptItem
        {
            TransactionId = transactionId,
            Description = description,
            ProductCode = productCode,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            ItemOrder = itemOrder,
        };

        item.SetAuditOnCreate(userId);
        return item;
    }
}