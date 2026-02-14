namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record TransactionHistoryEntry(TransactionResponse Transaction, string ActionType);
