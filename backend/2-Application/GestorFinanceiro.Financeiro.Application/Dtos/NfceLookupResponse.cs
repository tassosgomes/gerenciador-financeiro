namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record NfceLookupResponse(
    string AccessKey,
    string EstablishmentName,
    string EstablishmentCnpj,
    DateTime IssuedAt,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal PaidAmount,
    IReadOnlyList<ReceiptItemResponse> Items,
    bool AlreadyImported
);
