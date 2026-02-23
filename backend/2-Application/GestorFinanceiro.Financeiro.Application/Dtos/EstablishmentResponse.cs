namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record EstablishmentResponse(
    Guid Id,
    string Name,
    string Cnpj,
    string AccessKey
);
