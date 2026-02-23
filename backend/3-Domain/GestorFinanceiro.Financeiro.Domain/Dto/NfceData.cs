namespace GestorFinanceiro.Financeiro.Domain.Dto;

public record NfceData(
    string AccessKey,
    string EstablishmentName,
    string EstablishmentCnpj,
    DateTime IssuedAt,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal PaidAmount,
    IReadOnlyList<NfceItemData> Items
);

public record NfceItemData(
    string Description,
    string? ProductCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal TotalPrice
);
