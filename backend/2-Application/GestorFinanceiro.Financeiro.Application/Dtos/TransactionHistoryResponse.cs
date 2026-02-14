namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record TransactionHistoryResponse(IReadOnlyList<TransactionHistoryEntry> Entries);
