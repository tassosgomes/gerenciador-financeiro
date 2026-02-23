namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record TransactionReceiptResponse(
    EstablishmentResponse Establishment,
    IReadOnlyList<ReceiptItemResponse> Items
);
