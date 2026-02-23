using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Receipt;

public record ImportNfceCommand(
    string AccessKey,
    Guid AccountId,
    Guid CategoryId,
    string Description,
    DateTime CompetenceDate,
    string UserId,
    string? OperationId = null
) : ICommand<ImportNfceResponse>;
