namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record ImportNfceResponse(
    TransactionResponse Transaction,
    EstablishmentResponse Establishment,
    IReadOnlyList<ReceiptItemResponse> Items
);
