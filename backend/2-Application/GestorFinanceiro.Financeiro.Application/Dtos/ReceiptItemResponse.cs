namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record ReceiptItemResponse(
    Guid Id,
    string Description,
    string? ProductCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal TotalPrice,
    int ItemOrder
);
