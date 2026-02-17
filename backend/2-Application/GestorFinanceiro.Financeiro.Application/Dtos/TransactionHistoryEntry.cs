namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record TransactionHistoryEntry(
	TransactionResponse Transaction,
	string ActionType,
	string PerformedBy,
	DateTime PerformedAt,
	string? Details
);
