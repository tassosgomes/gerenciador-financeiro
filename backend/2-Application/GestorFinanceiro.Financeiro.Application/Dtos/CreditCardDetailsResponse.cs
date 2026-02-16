namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record CreditCardDetailsResponse(
    decimal CreditLimit,
    int ClosingDay,
    int DueDay,
    Guid DebitAccountId,
    bool EnforceCreditLimit,
    decimal AvailableLimit
);
